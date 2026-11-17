using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace MG.Sonarr.Next.Generators;

/// <summary>
/// Emits the list of JSON converters for the model classes that the services library marks with
/// <c>[SonarrObject]</c>, or the diagnostic that rejects a marked class.
/// </summary>
/// <remarks>
/// <para>
/// The generated <c>SonarrObjectConverters.Create</c> creates a <c>SonarrObjectConverter&lt;T&gt;</c> for each marked
/// class with an ordinary constructor call, so a connection's JSON options get their converters without reflection,
/// and the compiler holds each model to the converter's constraints.
/// </para>
/// <para>
/// The attribute, the models' base class, and the converters all belong to the services library, so the generator
/// finds them by their metadata names instead of injecting an attribute of its own, and a compilation that lacks them
/// gets nothing.
/// </para>
/// <para>
/// A marked class that the converter can't take is reported on its attribute and left out of the list, so the error
/// names the class, where a constraint violation in the generated file would point at a line the author never wrote.
/// With no class left to list, the generated class doesn't exist, so the code that creates the converters doesn't
/// compile, rather than a connection running without them.
/// </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class SonarrObjectConverterGenerator : IIncrementalGenerator
{
	/// <summary>The metadata name of the services library's marker attribute.</summary>
	private const string ATTRIBUTE_FULL_NAME = "MG.Sonarr.Next.Attributes.SonarrObjectAttribute";

	/// <summary>The metadata name of the class that every model derives from.</summary>
	private const string SONARR_OBJECT_FULL_NAME = "MG.Sonarr.Next.Models.SonarrObject";

	/// <summary>The metadata name of the interface whose static members give a model's name maps.</summary>
	private const string SERIALIZABLE_NAMES_FULL_NAME = "MG.Sonarr.Next.Json.ISerializableNames`1";

	/// <summary>The metadata name of the converter that reads and writes one model type.</summary>
	private const string MODEL_CONVERTER_FULL_NAME = "MG.Sonarr.Next.Json.Converters.SonarrObjectConverter`1";

	/// <summary>The metadata name of the shared converter that each model's converter is created around.</summary>
	private const string OBJECT_CONVERTER_FULL_NAME = "MG.Sonarr.Next.Json.Converters.ObjectConverter";

	private const string CATEGORY = "PoshSonarr.Usage";

	/// <summary>The namespace of the generated class, which is the converters' own.</summary>
	private const string GENERATED_NAMESPACE = "MG.Sonarr.Next.Json.Converters";

	/// <summary>The name of the generated class.</summary>
	private const string GENERATED_CLASS = "SonarrObjectConverters";

	private const string JSON_CONVERTER = "global::System.Text.Json.Serialization.JsonConverter";
	private const string MODEL_CONVERTER = "global::MG.Sonarr.Next.Json.Converters.SonarrObjectConverter";
	private const string OBJECT_CONVERTER = "global::MG.Sonarr.Next.Json.Converters.ObjectConverter";

	private static readonly DiagnosticDescriptor s_notConstructibleRule = new(
		id: "PSNR104",
		title: "Marked model type cannot be constructed by its converter",
		messageFormat: "'{0}' cannot be marked [SonarrObject] because it is {1} - its converter creates each model with 'new T()'",
		category: CATEGORY,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description:
			"[SonarrObject] gives the marked class a SonarrObjectConverter<T> of its own, which creates each model with "
			+ "T's public parameterless constructor before it adds a note property for each field of the JSON object, so "
			+ "the converter constrains T to new(). A static, abstract, or open generic class can't be that T, and neither "
			+ "can a class without a public parameterless constructor. The attribute isn't inherited, so marking an "
			+ "abstract base class gives the models that derive from it nothing; mark each concrete model class instead. "
			+ "The generator leaves the class out of its list of converters, so the error lands here rather than on a "
			+ "constraint in the generated file.");

	private static readonly DiagnosticDescriptor s_notModelRule = new(
		id: "PSNR105",
		title: "Marked type does not derive from SonarrObject",
		messageFormat: "'{0}' cannot be marked [SonarrObject] because it doesn't derive from SonarrObject - its converter reads and writes a model's note properties",
		category: CATEGORY,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description:
			"A SonarrObjectConverter<T> constrains T to SonarrObject, since it reads each field of a JSON object into a "
			+ "note property of the model and writes the note properties back. Derive the class from SonarrObject, "
			+ "IdSonarrObject<TSelf>, or TagUpdateObject<TSelf>, or take the attribute off a class that isn't a model. "
			+ "The generator leaves the class out of its list of converters, so the error lands here rather than on a "
			+ "constraint in the generated file.");

	private static readonly DiagnosticDescriptor s_noOwnNamesRule = new(
		id: "PSNR106",
		title: "Marked model type does not implement ISerializableNames of itself",
		messageFormat: "'{0}' cannot be marked [SonarrObject] because it doesn't implement ISerializableNames<{0}> - its converter reads the model's name maps through it",
		category: CATEGORY,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description:
			"A SonarrObjectConverter<T> constrains T to ISerializableNames<T>, through whose static members it reads the "
			+ "model's renamed and capitalized properties. A model that derives from another model inherits the base "
			+ "model's ISerializableNames, which doesn't meet the constraint, so it declares the interface again for "
			+ "itself, as in 'AddSeriesObject : SeriesObject, ISerializableNames<AddSeriesObject>'. The generator leaves "
			+ "the class out of its list of converters, so the error lands here rather than on a constraint in the "
			+ "generated file.");

	private static readonly DiagnosticDescriptor s_notAccessibleRule = new(
		id: "PSNR107",
		title: "Marked model type is not accessible to the generated converter list",
		messageFormat: "'{0}' cannot be marked [SonarrObject] because {1} - the generated list of converters sits at namespace scope, where it has to name every model",
		category: CATEGORY,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description:
			"The generated SonarrObjectConverters class sits at namespace scope in the model's assembly, so it names each "
			+ "model through every type that contains it. A private, protected, private protected, or file-local class, "
			+ "or a class nested in one, can't be named from there. Left to the generated file, that surfaces as an "
			+ "inaccessible-type error on a line the author never wrote; reporting it here puts it on the attribute. Make "
			+ "the class, and every type that contains it, internal or public.");

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// A record class is a RecordDeclarationSyntax, not a ClassDeclarationSyntax; a record struct is too, and
		// CreateResult turns it away by its type kind, though the attribute's usage already rejects it (CS0592).
		IncrementalValuesProvider<ModelResult> results = context.SyntaxProvider.ForAttributeWithMetadataName(
				ATTRIBUTE_FULL_NAME,
				static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
				static (ctx, _) => CreateResult(in ctx))
			.Where(static result => result is not null)!;

		context.RegisterSourceOutput(results, static (spc, result) =>
		{
			if (result.Diagnostic is not null)
			{
				spc.ReportDiagnostic(result.Diagnostic.ToDiagnostic());
			}
		});

		// The list changes only when a model is added, removed, or renamed, or starts or stops passing the checks, so an
		// edit inside a model's body doesn't render the file again.
		IncrementalValueProvider<EquatableArray<string>> models = results
			.Where(static result => result.ModelFullName is not null)
			.Select(static (result, _) => result.ModelFullName!)
			.Collect()
			.Select(static (names, _) => SortDistinct(names));

		context.RegisterSourceOutput(models, static (spc, names) =>
		{
			if (names.Length > 0)
			{
				spc.AddSource($"{GENERATED_CLASS}.g.cs", SourceText.From(Render(names), Encoding.UTF8));
			}
		});
	}

	// ── Model creation ───────────────────────────────────────────────────

	/// <summary>Reads one marked class into either the model to list or the diagnostic that rejects it.</summary>
	/// <remarks>
	/// The checks follow the converter's constraint, <c>where T : SonarrObject, ISerializableNames&lt;T&gt;, new()</c>,
	/// and the first that fails is the one reported, so a class that fails several reports one diagnostic at a time.
	/// </remarks>
	/// <param name="ctx">The context describing the marked class.</param>
	/// <returns>
	/// The result to act on, or <see langword="null"/> when the target isn't a class or the compilation lacks the types
	/// that the generated list names.
	/// </returns>
	private static ModelResult? CreateResult(in GeneratorAttributeSyntaxContext ctx)
	{
		if (ctx.TargetSymbol is not INamedTypeSymbol model || model.TypeKind != TypeKind.Class)
			return null;

		Compilation compilation = ctx.SemanticModel.Compilation;
		INamedTypeSymbol? sonarrObject = compilation.GetTypeByMetadataName(SONARR_OBJECT_FULL_NAME);
		INamedTypeSymbol? serializableNames = compilation.GetTypeByMetadataName(SERIALIZABLE_NAMES_FULL_NAME);

		if (sonarrObject is null
			|| serializableNames is null
			|| compilation.GetTypeByMetadataName(MODEL_CONVERTER_FULL_NAME) is null
			|| compilation.GetTypeByMetadataName(OBJECT_CONVERTER_FULL_NAME) is null)
		{
			return null;
		}

		LocationInfo? location = LocationInfo.From(ctx.Attributes[0].ApplicationSyntaxReference) ?? LocationInfo.From(ctx.TargetNode);

		if (TryGetNonConstructibleReason(model, out string reason))
		{
			return ModelResult.Fatal(new DiagnosticInfo(s_notConstructibleRule, location, model.Name, reason));
		}

		// A user-defined conversion can't target a base class or an interface, so an implicit conversion to either is
		// the implicit reference conversion that a constraint asks for.
		if (!compilation.HasImplicitConversion(model, sonarrObject))
		{
			return ModelResult.Fatal(new DiagnosticInfo(s_notModelRule, location, model.Name, string.Empty));
		}

		if (!compilation.HasImplicitConversion(model, serializableNames.Construct(model)))
		{
			return ModelResult.Fatal(new DiagnosticInfo(s_noOwnNamesRule, location, model.Name, string.Empty));
		}

		// The generated class is internal and sits in the model's own assembly, so the accessibility only has to stay
		// internal, and a model that can't be named from there is the one thing left to find.
		Accessibility accessibility = Accessibility.Internal;
		if (!SymbolAccessibility.TryNarrow(model, ref accessibility, out string hidden))
		{
			return ModelResult.Fatal(new DiagnosticInfo(s_notAccessibleRule, location, model.Name, hidden));
		}

		return new ModelResult(
			ModelFullName: model.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			Diagnostic: null);
	}

	/// <summary>Determines whether the converter would be unable to create the marked class.</summary>
	/// <param name="model">The marked class.</param>
	/// <param name="reason">
	/// When this method returns <see langword="true"/>, contains the phrase naming why; otherwise, an empty string.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the class can't meet the converter's <c>new()</c> constraint; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	private static bool TryGetNonConstructibleReason(INamedTypeSymbol model, out string reason)
	{
		// Static classes are abstract and sealed in metadata, so they are separated out before the abstract check.
		if (model.IsStatic)
		{
			reason = "a static class";
			return true;
		}

		if (model.IsAbstract)
		{
			reason = "abstract";
			return true;
		}

		// A class nested in a generic type counts as generic too, since the list would have to name its type arguments.
		if (model.IsGenericType)
		{
			reason = "an open generic type";
			return true;
		}

		if (!HasPublicParameterlessConstructor(model))
		{
			reason = "missing a public parameterless constructor";
			return true;
		}

		reason = string.Empty;
		return false;
	}

	/// <summary>Determines whether the marked class has the constructor that a <c>new()</c> constraint asks for.</summary>
	/// <remarks>
	/// A class that declares no constructor gets an implicit public one. A constructor whose parameters are all optional
	/// doesn't count, as it doesn't for the constraint.
	/// </remarks>
	/// <param name="model">The marked class.</param>
	/// <returns>
	/// <see langword="true"/> if the class has a public instance constructor without parameters; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	private static bool HasPublicParameterlessConstructor(INamedTypeSymbol model)
	{
		foreach (IMethodSymbol constructor in model.InstanceConstructors)
		{
			if (constructor.DeclaredAccessibility == Accessibility.Public && constructor.Parameters.IsEmpty)
				return true;
		}

		return false;
	}

	/// <summary>Sorts the names of the models to list, keeping each name once.</summary>
	/// <remarks>
	/// The ordinal order keeps the generated file the same whatever order the compiler visits the declarations in. A
	/// partial class marked on more than one of its declarations comes back once for each, which the compiler reports
	/// as a duplicate attribute, so the list keeps one.
	/// </remarks>
	/// <param name="names">The fully qualified names of the models that passed.</param>
	/// <returns>The sorted names, each once.</returns>
	private static EquatableArray<string> SortDistinct(ImmutableArray<string> names)
	{
		string[] sorted = names.Distinct(StringComparer.Ordinal).ToArray();
		Array.Sort(sorted, StringComparer.Ordinal);
		return new EquatableArray<string>(sorted);
	}

	// ── Rendering ────────────────────────────────────────────────────────

	/// <summary>Renders the class that creates a converter for each listed model.</summary>
	/// <param name="models">The models' fully qualified names, sorted.</param>
	/// <returns>The generated source text.</returns>
	private static string Render(EquatableArray<string> models)
	{
		StringBuilder sb = new(1_500 + (models.Length * 150));
		sb.AppendLine("// <auto-generated/>");
		sb.AppendLine("#nullable enable");
		sb.Append("namespace ").Append(GENERATED_NAMESPACE).AppendLine(";");
		sb.AppendLine();
		sb.AppendLine("/// <summary>");
		sb.AppendLine("/// Provides a converter of its own for each model class marked with <see cref=\"MG.Sonarr.Next.Attributes.SonarrObjectAttribute\"/>.");
		sb.AppendLine("/// </summary>");
		sb.Append("internal static class ").AppendLine(GENERATED_CLASS);
		sb.AppendLine("{");
		sb.AppendLine("\t/// <summary>");
		sb.AppendLine("\t/// Creates a <see cref=\"SonarrObjectConverter{T}\"/> for each model class marked with <see cref=\"MG.Sonarr.Next.Attributes.SonarrObjectAttribute\"/>.");
		sb.AppendLine("\t/// </summary>");
		sb.AppendLine("\t/// <param name=\"converter\">The connection's shared converter, which each model's converter reads and writes through.</param>");
		sb.AppendLine("\t/// <returns>A new array that holds one converter for each marked model class, in the ordinal order of the models' full names.</returns>");
		sb.AppendLine("\t/// <exception cref=\"System.NullReferenceException\">Thrown when <paramref name=\"converter\"/> is null.</exception>");
		sb.Append("\tinternal static ").Append(JSON_CONVERTER).Append("[] Create(").Append(OBJECT_CONVERTER).AppendLine(" converter)");
		sb.AppendLine("\t{");
		sb.Append("\t\treturn new ").Append(JSON_CONVERTER).AppendLine("[]");
		sb.AppendLine("\t\t{");

		foreach (string model in models)
		{
			sb.Append("\t\t\tnew ").Append(MODEL_CONVERTER).Append('<').Append(model).AppendLine(">(converter),");
		}

		sb.AppendLine("\t\t};");
		sb.AppendLine("\t}");
		sb.AppendLine("}");

		return sb.ToString();
	}

	// ── Models ───────────────────────────────────────────────────────────

	/// <summary>Carries what one marked class produces: the model to list, or the diagnostic that rejects it.</summary>
	/// <remarks>
	/// Every part of the result compares by value, so the incremental pipeline reuses its earlier output for a class that
	/// an edit leaves unchanged.
	/// </remarks>
	/// <param name="ModelFullName">
	/// The marked class's fully qualified name, or <see langword="null"/> when a diagnostic rejects it.
	/// </param>
	/// <param name="Diagnostic">The diagnostic to report, or <see langword="null"/> when there is none.</param>
	private sealed record ModelResult(string? ModelFullName, DiagnosticInfo? Diagnostic)
	{
		/// <summary>Creates a result that reports one blocking diagnostic and lists nothing.</summary>
		/// <param name="diagnostic">The diagnostic that rejects the class.</param>
		/// <returns>The result to report.</returns>
		public static ModelResult Fatal(DiagnosticInfo diagnostic)
		{
			return new ModelResult(ModelFullName: null, Diagnostic: diagnostic);
		}
	}
}
