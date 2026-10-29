using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace MG.Sonarr.Next.Generators;

/// <summary>
/// Emits the dependency injection registration extension method for a class marked with <c>[RegisterService]</c>.
/// </summary>
/// <remarks>
/// The generated method carries what a hand-written <c>Add&lt;Thing&gt;</c> extension would have, so a registration
/// declares itself next to the type it registers instead of in a separate file that has to be kept in step with it.
/// Nothing is reflected at startup - the emitted call site is an ordinary generic <c>AddSingleton</c>,
/// <c>AddScoped</c>, or <c>AddTransient</c> call from <c>Microsoft.Extensions.DependencyInjection</c>, which stays
/// trim-safe and costs nothing to build.
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class ServiceRegistrationGenerator : IIncrementalGenerator
{
	/// <summary>The metadata name of the injected marker attribute.</summary>
	internal const string ATTRIBUTE_FULL_NAME = "MG.Sonarr.Next.Generators.RegisterServiceAttribute";

	private const string CATEGORY = "PoshSonarr.Usage";

	/// <summary>The suffix trimmed from a type name before it becomes the default method name.</summary>
	private const string SERVICE_SUFFIX = "Service";

	private const string SERVICE_COLLECTION = "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";

	/// <summary>
	/// The type that declares the non-keyed <c>AddSingleton</c>, <c>AddScoped</c>, and <c>AddTransient</c> overloads.
	/// </summary>
	/// <remarks>
	/// They are reached through this static form rather than as extension methods, because the generated file carries
	/// no using directives.
	/// </remarks>
	private const string SERVICE_EXTENSIONS = "global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions";

	private static readonly DiagnosticDescriptor s_notConstructibleRule = new(
		id: "PSNR100",
		title: "Registered service type cannot be constructed by the container",
		messageFormat: "'{0}' cannot be registered by [RegisterService] because it is {1} - the container has to construct it",
		category: CATEGORY,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description:
			"[RegisterService] registers the marked type as the implementation, so the container instantiates it on first "
			+ "resolve. An abstract, static, or open generic type makes BuildServiceProvider throw ArgumentException - "
			+ "'Cannot instantiate implementation type' - which fails the whole container rather than just this "
			+ "registration. Mark the concrete type instead and expose the abstraction through the attribute's service "
			+ "type argument. A type with no public constructor fails the same way once ValidateOnBuild runs - 'A suitable "
			+ "constructor for type ... could not be located' - because the container only calls public constructors, "
			+ "whatever the accessibility of the type itself; make one public.");

	private static readonly DiagnosticDescriptor s_notAssignableRule = new(
		id: "PSNR101",
		title: "Registered service type is not a base type of the implementation",
		messageFormat: "'{0}' cannot be the service type for '{1}' - the service type must be '{1}' itself, a base class of it, or an interface it implements",
		category: CATEGORY,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description:
			"The type passed to [RegisterService] becomes the contract the registration is resolved by, and the marked "
			+ "type becomes its implementation, so the implementation has to be assignable to it. Reporting the mismatch "
			+ "here puts the error on the attribute; left to the generated file, it surfaces as a generic constraint "
			+ "violation on a call site the author never wrote.");

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// 1) Inject the marker attribute, so no shared "Abstractions" project is needed to reach it.
		context.RegisterPostInitializationOutput(static ctx =>
		{
			ctx.AddSource(
				"RegisterServiceAttribute.g.cs",
				SourceText.From(ATTRIBUTE_SOURCE, Encoding.UTF8));
		});

		// 2) Find classes decorated with [RegisterService] → emit their registration extension.
		IncrementalValuesProvider<RegistrationResult> services = context.SyntaxProvider.ForAttributeWithMetadataName(
				ATTRIBUTE_FULL_NAME,
				static (node, _) => node is ClassDeclarationSyntax,
				static (ctx, _) => CreateResult(in ctx))
			.Where(static result => result is not null)!;

		context.RegisterSourceOutput(services, static (spc, result) =>
		{
			if (result.Diagnostic is not null)
			{
				spc.ReportDiagnostic(result.Diagnostic.ToDiagnostic());
			}

			if (result.Model is not ServiceRegistrationModel model)
			{
				return;
			}

			spc.AddSource($"{model.HintName}.ServiceRegistration.g.cs", SourceText.From(Render(model), Encoding.UTF8));
		});
	}

	// ── Model creation ───────────────────────────────────────────────────

	/// <summary>Reads one attributed class into either a registration model or the diagnostic that rejects it.</summary>
	/// <param name="ctx">The context describing the attributed class.</param>
	/// <returns>The result to act on, or <see langword="null"/> when the target is not a class this generator handles.</returns>
	private static RegistrationResult? CreateResult(in GeneratorAttributeSyntaxContext ctx)
	{
		if (ctx.TargetSymbol is not INamedTypeSymbol implementation || implementation.TypeKind != TypeKind.Class)
			return null;

		AttributeData attribute = ctx.Attributes[0];
		LocationInfo? location = LocationInfo.From(attribute.ApplicationSyntaxReference) ?? LocationInfo.From(ctx.TargetNode);

		if (TryGetNonConstructibleReason(implementation, out string reason))
		{
			return RegistrationResult.Fatal(new DiagnosticInfo(s_notConstructibleRule, location, implementation.Name, reason));
		}

		ReadConstructorArguments(attribute, out INamedTypeSymbol? serviceType, out int lifetime);

		if (serviceType is not null && !IsAssignableTo(implementation, serviceType))
		{
			return RegistrationResult.Fatal(new DiagnosticInfo(s_notAssignableRule, location, serviceType.Name, implementation.Name));
		}

		ReadNamedArguments(attribute, out string? methodName);

		string? ns = implementation.ContainingNamespace?.IsGlobalNamespace == true
			? null
			: implementation.ContainingNamespace?.ToDisplayString();

		string implementationName = implementation.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

		// A contract identical to the implementation is the self-registering shape written the long way, so it
		// collapses onto the single-argument overload rather than emitting AddSingleton<T, T>.
		bool selfRegistering = serviceType is null || SymbolEqualityComparer.Default.Equals(serviceType, implementation);

		return new RegistrationResult(
			Model: new ServiceRegistrationModel(
				Namespace: ns,
				ClassName: GetRegistrationClassName(implementation),
				MethodName: methodName ?? GetDefaultMethodName(implementation.Name),
				ImplementationFullName: implementationName,
				ServiceFullName: selfRegistering ? null : serviceType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				AddMethodName: GetAddMethodName(lifetime),
				LifetimeWord: GetLifetimeWord(lifetime),
				HintName: MakeHintName(implementationName)),
			Diagnostic: null);
	}

	/// <summary>Reads the attribute's constructor arguments by parameter name.</summary>
	/// <remarks>
	/// The overloads put the two parameters in different positions, so positional indexing would misread them. The
	/// parameter list is walked in step with the argument list instead, which stays correct as overloads are added.
	/// </remarks>
	/// <param name="attribute">The applied attribute.</param>
	/// <param name="serviceType">
	/// When this method returns, contains the contract the registration is resolved by, or <see langword="null"/> when
	/// the marked type registers as its own.
	/// </param>
	/// <param name="lifetime">When this method returns, contains the lifetime's numeric value, defaulting to singleton.</param>
	private static void ReadConstructorArguments(AttributeData attribute, out INamedTypeSymbol? serviceType, out int lifetime)
	{
		serviceType = null;
		lifetime = 0;

		if (attribute.AttributeConstructor is not IMethodSymbol constructor)
		{
			return;
		}

		ImmutableArray<IParameterSymbol> parameters = constructor.Parameters;
		ImmutableArray<TypedConstant> arguments = attribute.ConstructorArguments;

		// A malformed attribute can leave the two out of step, so the shorter one bounds the walk.
		int count = Math.Min(parameters.Length, arguments.Length);

		for (int i = 0; i < count; i++)
		{
			switch (parameters[i].Name)
			{
				case "serviceType":
					serviceType = arguments[i].Value as INamedTypeSymbol;
					break;

				// The enum arrives boxed as its underlying int.
				case "lifetime":
					lifetime = arguments[i].Value as int? ?? 0;
					break;
			}
		}
	}

	/// <summary>Reads the attribute's optional <c>MethodName</c> named argument.</summary>
	/// <param name="attribute">The applied attribute.</param>
	/// <param name="methodName">When this method returns, contains the explicit method name, or <see langword="null"/> to derive one.</param>
	private static void ReadNamedArguments(AttributeData attribute, out string? methodName)
	{
		methodName = null;

		foreach (KeyValuePair<string, TypedConstant> named in attribute.NamedArguments)
		{
			if (named.Key != "MethodName")
			{
				continue;
			}

			// An empty or whitespace name would render an unspeakable method, so it falls back to the default.
			string? name = named.Value.Value as string;
			methodName = string.IsNullOrWhiteSpace(name) ? null : name;
		}
	}

	/// <summary>Determines whether the container would be unable to instantiate the marked type.</summary>
	/// <param name="implementation">The marked type.</param>
	/// <param name="reason">When this method returns <see langword="true"/>, contains the phrase naming why; otherwise, an empty string.</param>
	/// <returns><see langword="true"/> if the type cannot be constructed; otherwise, <see langword="false"/>.</returns>
	private static bool TryGetNonConstructibleReason(INamedTypeSymbol implementation, out string reason)
	{
		// Static classes are abstract and sealed in metadata, so they are separated out before the abstract check.
		if (implementation.IsStatic)
		{
			reason = "a static class";
			return true;
		}

		if (implementation.IsAbstract)
		{
			reason = "abstract";
			return true;
		}

		if (implementation.IsGenericType)
		{
			// An open generic can only be registered through the non-generic Add overloads that take a Type, which is
			// a different shape than the one emitted here.
			reason = "an open generic type";
			return true;
		}

		if (!HasPublicConstructor(implementation))
		{
			reason = "missing a public constructor";
			return true;
		}

		reason = string.Empty;
		return false;
	}

	/// <summary>Determines whether the container can find a constructor on the marked type.</summary>
	/// <remarks>
	/// The container reads constructors through <c>Type.GetConstructors()</c>, so only a public instance constructor
	/// counts, whatever the accessibility of the type itself. A class that declares none gets an implicit public one.
	/// </remarks>
	/// <param name="implementation">The marked type.</param>
	/// <returns><see langword="true"/> if the type has a public instance constructor; otherwise, <see langword="false"/>.</returns>
	private static bool HasPublicConstructor(INamedTypeSymbol implementation)
	{
		foreach (IMethodSymbol constructor in implementation.InstanceConstructors)
		{
			if (constructor.DeclaredAccessibility == Accessibility.Public)
				return true;
		}

		return false;
	}

	/// <summary>Determines whether <paramref name="implementation"/> can be resolved as <paramref name="serviceType"/>.</summary>
	/// <param name="implementation">The marked type.</param>
	/// <param name="serviceType">The contract named by the attribute.</param>
	/// <returns><see langword="true"/> if the implementation is the service type, derives from it, or implements it; otherwise, <see langword="false"/>.</returns>
	private static bool IsAssignableTo(INamedTypeSymbol implementation, INamedTypeSymbol serviceType)
	{
		if (SymbolEqualityComparer.Default.Equals(implementation, serviceType))
			return true;

		if (serviceType.TypeKind == TypeKind.Interface)
		{
			foreach (INamedTypeSymbol iface in implementation.AllInterfaces)
			{
				if (SymbolEqualityComparer.Default.Equals(iface, serviceType))
					return true;
			}

			return false;
		}

		for (INamedTypeSymbol? baseType = implementation.BaseType; baseType is not null; baseType = baseType.BaseType)
		{
			if (SymbolEqualityComparer.Default.Equals(baseType, serviceType))
				return true;
		}

		return false;
	}

	/// <summary>Builds the default method name from the marked type's name.</summary>
	/// <remarks>
	/// A trailing <c>Service</c> is trimmed, so <c>ClockService</c> registers through <c>AddClock</c>. A type named
	/// exactly <c>Service</c> keeps it, because trimming would leave nothing behind.
	/// </remarks>
	/// <param name="typeName">The marked type's name.</param>
	/// <returns>The method name to emit.</returns>
	private static string GetDefaultMethodName(string typeName)
	{
		string trimmed = typeName.Length > SERVICE_SUFFIX.Length && typeName.EndsWith(SERVICE_SUFFIX, StringComparison.Ordinal)
			? typeName.Substring(0, typeName.Length - SERVICE_SUFFIX.Length)
			: typeName;

		return "Add" + trimmed;
	}

	/// <summary>Builds the name of the generated static class that holds the extension method.</summary>
	/// <remarks>
	/// An extension method has to live in a non-nested static class, so a marked nested type still emits at namespace
	/// scope; its containing type names are folded into the class name to keep two like-named nested types apart.
	/// </remarks>
	/// <param name="implementation">The marked type.</param>
	/// <returns>The generated class name.</returns>
	private static string GetRegistrationClassName(INamedTypeSymbol implementation)
	{
		StringBuilder sb = new(implementation.Name.Length + 12);
		AppendContainingTypeNames(sb, implementation.ContainingType);
		return sb.Append(implementation.Name).Append("Registration").ToString();

		static void AppendContainingTypeNames(StringBuilder sb, INamedTypeSymbol? containing)
		{
			if (containing is null)
				return;

			AppendContainingTypeNames(sb, containing.ContainingType);
			sb.Append(containing.Name);
		};
	}

	/// <summary>Maps the attribute's lifetime value onto the extension method that registers it.</summary>
	/// <param name="lifetime">The lifetime's numeric value.</param>
	/// <returns>The name of the matching registration method.</returns>
	private static string GetAddMethodName(int lifetime)
	{
		switch (lifetime)
		{
			case 1: return "AddScoped";
			case 2: return "AddTransient";
			default: return "AddSingleton";
		}
	}

	/// <summary>Maps the attribute's lifetime value onto the word used for it in the generated documentation.</summary>
	/// <param name="lifetime">The lifetime's numeric value.</param>
	/// <returns>The lifetime's lowercase name.</returns>
	private static string GetLifetimeWord(int lifetime)
	{
		switch (lifetime)
		{
			case 1: return "scoped";
			case 2: return "transient";
			default: return "singleton";
		}
	}

	/// <summary>Builds a unique, stable file hint name from a fully qualified type name.</summary>
	/// <param name="fullyQualifiedName">The marked type's fully qualified name.</param>
	/// <returns>The hint name, carrying no characters that are illegal in a file name.</returns>
	private static string MakeHintName(string fullyQualifiedName)
	{
		return fullyQualifiedName.Replace("global::", "")
								 .Replace('<', '_')
								 .Replace('>', '_')
								 .Replace('.', '_')
								 .Replace('+', '_');
	}

	/// <summary>Strips the global alias from a fully qualified name, so it can be written into a <c>cref</c>.</summary>
	/// <param name="fullyQualifiedName">The name to strip.</param>
	/// <returns>The name without its <c>global::</c> prefix.</returns>
	private static string Speakable(string fullyQualifiedName)
	{
		const string global_alias = "global::";
		return fullyQualifiedName.StartsWith(global_alias, StringComparison.Ordinal)
			? fullyQualifiedName.Substring(global_alias.Length)
			: fullyQualifiedName;
	}

	// ── Rendering ────────────────────────────────────────────────────────

	/// <summary>Renders the registration extension class for one marked type.</summary>
	/// <param name="model">The registration being rendered.</param>
	/// <returns>The generated source text.</returns>
	private static string Render(ServiceRegistrationModel model)
	{
		string implementation = Speakable(model.ImplementationFullName);
		string? service = model.ServiceFullName is null ? null : Speakable(model.ServiceFullName);

		StringBuilder sb = new(2_000);
		sb.AppendLine("// <auto-generated/>");
		sb.AppendLine("#nullable enable");

		if (model.Namespace is not null)
		{
			sb.Append("namespace ").Append(model.Namespace).AppendLine(";");
			sb.AppendLine();
		}

		sb.Append("/// <summary>Provides the generated dependency injection registration for <see cref=\"")
		  .Append(implementation).AppendLine("\"/>.</summary>");
		sb.Append("public static class ").AppendLine(model.ClassName);
		sb.AppendLine("{");

		sb.Append("\t/// <summary>Registers <see cref=\"").Append(implementation).Append("\"/> as ");

		if (service is null)
		{
			sb.Append("a ").Append(model.LifetimeWord).Append(" service");
		}
		else
		{
			sb.Append("the ").Append(model.LifetimeWord).Append(" implementation of <see cref=\"")
			  .Append(service).Append("\"/>");
		}

		sb.AppendLine(".</summary>");
		sb.AppendLine("\t/// <param name=\"services\">The service collection to add the registration to.</param>");
		sb.Append("\t/// <returns>The same <see cref=\"").Append(Speakable(SERVICE_COLLECTION))
		  .AppendLine("\"/> instance so that additional calls can be chained.</returns>");

		sb.Append("\tpublic static ").Append(SERVICE_COLLECTION).Append(' ').Append(model.MethodName)
		  .Append("(this ").Append(SERVICE_COLLECTION).AppendLine(" services)");
		sb.AppendLine("\t{");

		RenderBody(sb, model);

		sb.AppendLine("\t}");
		sb.AppendLine("}");

		return sb.ToString();
	}

	/// <summary>Renders the single statement that makes up the registration method's body.</summary>
	/// <remarks>
	/// The statement calls the lifetime's registration method with the contract and the implementation as type
	/// arguments, or with the implementation alone when the marked type registers as its own.
	/// </remarks>
	/// <param name="sb">The builder to append to.</param>
	/// <param name="model">The registration being rendered.</param>
	private static void RenderBody(StringBuilder sb, ServiceRegistrationModel model)
	{
		sb.Append("\t\treturn ")
		  .Append(SERVICE_EXTENSIONS)
		  .Append('.')
		  .Append(model.AddMethodName)
		  .Append('<');

		if (model.ServiceFullName is not null)
		{
			sb.Append(model.ServiceFullName).Append(", ");
		}

		sb.Append(model.ImplementationFullName).AppendLine(">(services);");
	}

	// ── Attribute source ─────────────────────────────────────────────────

	private const string ATTRIBUTE_SOURCE = """
// <auto-generated/>
#nullable enable
namespace MG.Sonarr.Next.Generators
{
	/// <summary>
	/// Marks a class as a dependency injection service and generates the extension method that registers it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The generated method lands in the marked type's own namespace and is named <c>Add</c> plus the type name with a
	/// trailing <c>Service</c> trimmed, unless <see cref="MethodName"/> names it. The method registers the marked type
	/// as the implementation, so the type has to be one the container can construct.
	/// </para>
	/// <para>
	/// A registration is a singleton unless it passes a <see cref="ServiceRegistrationLifetime"/>.
	/// </para>
	/// </remarks>
	[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	internal sealed class RegisterServiceAttribute : global::System.Attribute
	{
		/// <summary>
		/// Initializes a new <see cref="RegisterServiceAttribute"/> instance that registers the marked type as its own
		/// service type.
		/// </summary>
		public RegisterServiceAttribute() { }

		/// <summary>
		/// Initializes a new <see cref="RegisterServiceAttribute"/> instance that registers the marked type as the
		/// implementation of the specified service type.
		/// </summary>
		/// <param name="serviceType">
		/// The contract the registration is resolved by. Must be the marked type itself, a base class of it, or an
		/// interface it implements.
		/// </param>
		public RegisterServiceAttribute(global::System.Type serviceType) { }

		/// <summary>
		/// Initializes a new <see cref="RegisterServiceAttribute"/> instance that registers the marked type as its own
		/// service type with the specified lifetime.
		/// </summary>
		/// <param name="lifetime">The lifetime the registration is added with.</param>
		public RegisterServiceAttribute(ServiceRegistrationLifetime lifetime) { }

		/// <summary>
		/// Initializes a new <see cref="RegisterServiceAttribute"/> instance that registers the marked type as the
		/// implementation of the specified service type with the specified lifetime.
		/// </summary>
		/// <param name="serviceType">
		/// The contract the registration is resolved by. Must be the marked type itself, a base class of it, or an
		/// interface it implements.
		/// </param>
		/// <param name="lifetime">The lifetime the registration is added with.</param>
		public RegisterServiceAttribute(global::System.Type serviceType, ServiceRegistrationLifetime lifetime) { }

		/// <summary>Gets or sets the name of the generated extension method.</summary>
		/// <value>
		/// The method name to emit, or <see langword="null"/> to derive one from the marked type's name. The default is
		/// <see langword="null"/>.
		/// </value>
		public string? MethodName { get; set; }
	}

	/// <summary>Names the lifetime a <see cref="RegisterServiceAttribute"/> registration is added with.</summary>
	/// <remarks>
	/// This mirrors <c>Microsoft.Extensions.DependencyInjection.ServiceLifetime</c> rather than naming it, so that the
	/// injected attribute compiles in any assembly that references the generator, including one that never took a
	/// dependency on the container. There is deliberately no <c>Singleton</c> member: a singleton is what a
	/// registration that passes no lifetime already is.
	/// </remarks>
	internal enum ServiceRegistrationLifetime
	{
		/// <summary>One instance per dependency injection scope.</summary>
		Scoped = 1,

		/// <summary>A new instance on every resolve.</summary>
		Transient = 2,
	}
}
""";

	// ── Models ───────────────────────────────────────────────────────────

	/// <summary>Carries what one attributed class produces: the registration to emit, or the diagnostic that rejects it.</summary>
	/// <remarks>
	/// Every part of the result compares by value, so the incremental pipeline skips reporting and rendering for a class
	/// that an edit leaves unchanged.
	/// </remarks>
	/// <param name="Model">The registration to emit, or <see langword="null"/> when a diagnostic rejects it.</param>
	/// <param name="Diagnostic">The diagnostic to report, or <see langword="null"/> when there is none.</param>
	private sealed record RegistrationResult(ServiceRegistrationModel? Model, DiagnosticInfo? Diagnostic)
	{
		/// <summary>Creates a result that reports one blocking diagnostic and emits nothing.</summary>
		/// <param name="diagnostic">The diagnostic that rejects the registration.</param>
		/// <returns>The result to report.</returns>
		public static RegistrationResult Fatal(DiagnosticInfo diagnostic)
		{
			return new RegistrationResult(Model: null, Diagnostic: diagnostic);
		}
	}

	/// <summary>Describes one registration to render, as plain values that the incremental cache can compare.</summary>
	/// <param name="Namespace">The marked type's namespace, or <see langword="null"/> for the global namespace.</param>
	/// <param name="ClassName">The name of the generated static class.</param>
	/// <param name="MethodName">The name of the generated extension method.</param>
	/// <param name="ImplementationFullName">The marked type's fully qualified name.</param>
	/// <param name="ServiceFullName">
	/// The contract's fully qualified name, or <see langword="null"/> when the marked type registers as its own.
	/// </param>
	/// <param name="AddMethodName">The name of the registration method that the lifetime calls for.</param>
	/// <param name="LifetimeWord">The lifetime's lowercase name, for the generated documentation.</param>
	/// <param name="HintName">The file hint name for the generated source.</param>
	private sealed record ServiceRegistrationModel(
		string? Namespace,
		string ClassName,
		string MethodName,
		string ImplementationFullName,
		string? ServiceFullName,
		string AddMethodName,
		string LifetimeWord,
		string HintName);

	/// <summary>Carries a diagnostic through the incremental pipeline without rooting a syntax tree.</summary>
	/// <param name="Descriptor">The rule being reported.</param>
	/// <param name="Location">The position to report it at, or <see langword="null"/> for no position.</param>
	/// <param name="Argument0">The first message argument.</param>
	/// <param name="Argument1">The second message argument.</param>
	private sealed record DiagnosticInfo(
		DiagnosticDescriptor Descriptor,
		LocationInfo? Location,
		string Argument0,
		string Argument1)
	{
		/// <summary>Materializes the reportable diagnostic.</summary>
		/// <returns>The diagnostic to report.</returns>
		public Diagnostic ToDiagnostic()
		{
			return Diagnostic.Create(this.Descriptor, this.Location?.ToLocation(), this.Argument0, this.Argument1);
		}
	}

	/// <summary>Records a source position as plain values, so the incremental cache holds no syntax tree.</summary>
	/// <param name="FilePath">The path of the file the position lives in.</param>
	/// <param name="TextSpan">The character span within that file.</param>
	/// <param name="LineSpan">The line and column span within that file.</param>
	private sealed record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
	{
		/// <summary>Rebuilds the <see cref="Location"/> this position was taken from.</summary>
		/// <returns>The reconstructed location.</returns>
		public Location ToLocation()
		{
			return Location.Create(this.FilePath, this.TextSpan, this.LineSpan);
		}

		/// <summary>Captures the position of a syntax node.</summary>
		/// <param name="node">The node to take the position of.</param>
		/// <returns>The captured position, or <see langword="null"/> when the node has no source position.</returns>
		public static LocationInfo? From(SyntaxNode? node)
		{
			return From(node?.GetLocation());
		}

		/// <summary>Captures the position an attribute was applied at.</summary>
		/// <param name="reference">The attribute's syntax reference.</param>
		/// <returns>The captured position, or <see langword="null"/> when the attribute has no source position.</returns>
		public static LocationInfo? From(SyntaxReference? reference)
		{
			return reference is null ? null : From(Location.Create(reference.SyntaxTree, reference.Span));
		}

		/// <summary>Captures the position of a location that lives in source.</summary>
		/// <param name="location">The location to take the position of.</param>
		/// <returns>The captured position, or <see langword="null"/> when <paramref name="location"/> is not in a syntax tree.</returns>
		private static LocationInfo? From(Location? location)
		{
			return location?.SourceTree is null
				? null
				: new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
		}
	}
}
