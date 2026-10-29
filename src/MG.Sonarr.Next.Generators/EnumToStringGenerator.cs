using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace MG.Sonarr.Next.Generators;

/// <summary>
/// Emits the conversion, validation, and parsing extension members for an enum marked with <c>[GenerateEnumToString]</c>.
/// </summary>
/// <remarks>
/// <para>
/// An enum named <c>Foo</c> gets a static <c>FooExtensions</c> class in its namespace, with its accessibility, whose
/// members stand in for the general-purpose <see cref="Enum"/> APIs: <c>TryGetName</c> and <c>ToStringFast</c> for
/// <see cref="Enum.ToString()"/>, <c>TryFormat</c> for UTF-16 and UTF-8 formatting, <c>IsValidEnum</c> for
/// <see cref="Enum.IsDefined(Type, object)"/>, and <c>TryParseFast</c> and <c>ParseFast</c> for parsing.
/// </para>
/// <para>
/// Where the consuming compilation can reach the module's unchecked span helpers, the formatting members copy through
/// them instead of the bounds-checked span methods, because their own guards have already settled the range.
/// </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class EnumToStringGenerator : IIncrementalGenerator
{
	/// <summary>The metadata name of the injected marker attribute.</summary>
	internal const string ATTRIBUTE_FULL_NAME = "MG.Sonarr.Next.Generators.GenerateEnumToStringAttribute";

	/// <summary>The metadata name probed to decide whether the consuming compilation can reach the bounds-check-free span helpers.</summary>
	private const string SPAN_UNSAFE_EXTENSIONS_METADATA_NAME = "MG.Sonarr.Next.Extensions.SpanUnsafeExtensions";

	/// <summary>
	/// The fully qualified form of <see cref="SPAN_UNSAFE_EXTENSIONS_METADATA_NAME"/>, as written into generated source.
	/// </summary>
	/// <remarks>
	/// Its members are C# 14 extension members, reached through their speakable static forms rather than as extension
	/// calls, because the generated file carries no using directives.
	/// </remarks>
	private const string SPAN_UNSAFE_EXTENSIONS = "global::MG.Sonarr.Next.Extensions.SpanUnsafeExtensions";

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// 1) Inject the attribute into the user's compilation (so they don't need a shared "Abstractions" project).
		context.RegisterPostInitializationOutput(static ctx =>
		{
			ctx.AddSource(
				"GenerateEnumToStringAttribute.g.cs",
				SourceText.From(ATTRIBUTE_SOURCE, Encoding.UTF8));
		});

		// 2) Find enums with [GenerateEnumToString].
		IncrementalValuesProvider<EnumModel?> enums = context.SyntaxProvider.ForAttributeWithMetadataName(
				ATTRIBUTE_FULL_NAME,
				static (node, _) => node is EnumDeclarationSyntax,
				static (ctx, _) => CreateEnumModel(in ctx))
			.Where(static model => model is not null);

		// 3) Note whether the consuming compilation can reach SpanUnsafeExtensions, which lets the formatting
		//    members drop the bounds checks their own guards have already settled. The class is internal, so
		//    reaching it is a matter of access rather than of a public declaration: MG.Sonarr.Next reaches it as
		//    its own, and each assembly that its InternalsVisibleTo names reaches it through the reference. The
		//    compilation itself changes on every keystroke, so it is reduced to a bool here: the incremental
		//    engine compares that instead, and generation only re-runs when that access appears or disappears.
		IncrementalValueProvider<bool> hasSpanUnsafeExtensions = context.CompilationProvider.Select(static (compilation, _) =>
		{
			INamedTypeSymbol? helpers = compilation.GetTypeByMetadataName(SPAN_UNSAFE_EXTENSIONS_METADATA_NAME);
			return helpers is not null && compilation.IsSymbolAccessibleWithin(helpers, compilation.Assembly);
		});

		// 4) Generate source per enum.
		context.RegisterSourceOutput(enums.Combine(hasSpanUnsafeExtensions), static (spc, pair) =>
		{
			EnumModel model = pair.Left!;
			string src = Render(model, pair.Right);
			spc.AddSource($"{model.HintName}.EnumToString.g.cs", SourceText.From(src, Encoding.UTF8));
		});
	}

	/// <summary>Reads one attributed enum into the model that its extensions class renders from.</summary>
	/// <param name="ctx">The context describing the attributed enum.</param>
	/// <returns>
	/// The model to render, or <see langword="null"/> when the target isn't a public or internal enum that declares at
	/// least one member.
	/// </returns>
	private static EnumModel? CreateEnumModel(in GeneratorAttributeSyntaxContext ctx)
	{
		if (ctx.TargetSymbol is not INamedTypeSymbol enumSymbol || enumSymbol.TypeKind != TypeKind.Enum
			||
			!(enumSymbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal))
			return null;

		// Only constants that are actual enum members (skip value__).
		EnumMember[] members = GetMembers(enumSymbol);

		// If there's nothing to generate, bail.
		if (members.Length == 0)
			return null;

		string? ns = enumSymbol.ContainingNamespace?.IsGlobalNamespace == true
			? null
			: enumSymbol.ContainingNamespace?.ToDisplayString();

		const string FLAGS_ATTRIBUTE_FULL_NAME = "System.FlagsAttribute";
		bool isFlags = false;
		bool disableDict = false;
		ImmutableArray<AttributeData> atts = ctx.TargetSymbol.GetAttributes();
		foreach (AttributeData ad in atts.AsSpan())
		{
			if (FLAGS_ATTRIBUTE_FULL_NAME.Equals(ad.AttributeClass?.ToDisplayString(), StringComparison.Ordinal))
			{
				isFlags = true;
				continue;
			}

			if (!ATTRIBUTE_FULL_NAME.Equals(ad.AttributeClass?.ToDisplayString(), StringComparison.Ordinal)
				||
				ad.NamedArguments.IsDefaultOrEmpty
				||
				!"DisableStringToEnumDictionary".Equals(ad.NamedArguments[0].Key, StringComparison.Ordinal))
			{
				continue;
			}

			disableDict = ad.NamedArguments[0].Value.Value as bool? ?? false;
		}

		return new EnumModel(
			Namespace: ns,
			EnumName: enumSymbol.Name,
			EnumFullType: enumSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			UnderlyingKeyword: GetUnderlyingKeyword(enumSymbol.EnumUnderlyingType),
			AllNamesAscii: AllNamesAreAscii(members),
			IsFlags: isFlags,
			DisableDictionary: disableDict,
			Accessibility: enumSymbol.DeclaredAccessibility,
			Members: members,
			HintName: MakeHintName(enumSymbol));
	}

	/// <summary>
	/// Renders the complete extensions class for a single enum.
	/// </summary>
	/// <param name="model">The enum being rendered.</param>
	/// <param name="useSpanUnsafeExtensions">A value that indicates whether the consuming compilation can reach <c>SpanUnsafeExtensions</c>.</param>
	/// <returns>The generated source text.</returns>
	private static string Render(EnumModel model, bool useSpanUnsafeExtensions)
	{
		StringBuilder sb = new(16_000);
		sb.AppendLine("// <auto-generated/>");
		sb.AppendLine("#nullable enable");

		if (model.Namespace is not null)
		{
			sb.Append("namespace ").Append(model.Namespace).AppendLine(";");
			sb.AppendLine();
		}

		// Match enum accessibility (public/internal).
		string access = model.Accessibility == Accessibility.Public ? "public" : "internal";
		string type = model.EnumFullType;

		// Two independent decisions. DisableStringToEnumDictionary suppresses the string-to-enum
		// members outright; separately, a name set at or under the threshold parses from a generated
		// comparison chain rather than a frozen table. A set this small resolves to a
		// LengthBucketsFrozenDictionary - bucket by length, then scan the bucket with the same ordinal
		// comparison the chain emits - so the table buys no better lookup while still costing its
		// construction and retained storage.
		const int name_table_threshold = 8;
		bool emitsParsing = !model.DisableDictionary;
		bool usesNameTable = emitsParsing && model.Members.Length > name_table_threshold;

		// Budget ", " between every name so the constant also covers a comma-separated listing.
		int totalLength = 0;
		foreach (EnumMember m in model.Members)
		{
			totalLength += m.Name.Length;
		}

		totalLength += Math.Max(0, model.Members.Length - 1) * 2;

		sb.Append("/// <summary>Provides fast, allocation-aware extension members for the <see cref=\"")
		  .Append(type)
		  .AppendLine("\"/> enumeration.</summary>");
		sb.Append(access).Append(" static partial class ").Append(model.EnumName).AppendLine("Extensions");
		sb.AppendLine("{");

		// Storage stays at class scope: an extension block accepts methods, properties, and indexers,
		// but never fields or constants (CS9282). The members inside the block read these unqualified.
		// Both public constants are deliberately left bare, with no extension-property counterpart: a
		// const is substituted at the call site, so a property fronting one could at best inline back to
		// the value the const already supplies for free, while giving up const initializers and every
		// other compile-time constant position.
		sb.AppendLine("\t/// <summary>Gets the total number of unique enum values with defined names for this enum type.</summary>");
		sb.Append("\tpublic const int ValueCount = ").Append(model.Members.Length).AppendLine(";");
		sb.AppendLine();

		sb.AppendLine("\t/// <summary>Gets the constant maximum length of the strings returned by <see cref=\"ToStringFast\"/> for this enum type.</summary>");
		sb.AppendLine("\t/// <remarks>Useful for sizing buffers when formatting enum values as strings.</remarks>");
		sb.Append("\tpublic const int MaxToStringFastLength = ").Append(totalLength).AppendLine(";");
		sb.AppendLine();

		if (model.IsFlags)
		{
			// Flags enums: valid iff every set bit belongs to a defined flag. Fold all members into a
			// mask (idempotent for aliases/combined members), then reject any value with out-of-mask bits.
			sb.Append("\tprivate const ").Append(type).Append(" DEFINED_FLAGS_MASK = ")
			  .Append(type).Append('.').Append(model.Members[0].Name);

			foreach (EnumMember fm in model.Members.AsSpan(1, model.Members.Length - 1))
			{
				sb.Append(" | ").Append(type).Append('.').Append(fm.Name);
			}

			sb.AppendLine(";");
			sb.AppendLine();
		}

		if (usesNameTable)
		{
			RenderNameTable(sb, model, type);
		}

		// One extension block carries everything callable: the instance members bind to the receiver,
		// the static members ignore it and surface as TEnum.Member. Instance extension methods keep a
		// speakable static form on the enclosing class, so XExtensions.ToStringFast(v) and
		// &XExtensions.ToStringFast still bind.
		sb.AppendLine("\t/// <summary>Provides fast conversion, validation, and parsing members for this enumeration.</summary>");
		sb.AppendLine("\t/// <param name=\"value\">The enum value that receives the instance members.</param>");
		sb.Append("\textension(").Append(type).AppendLine(" value)");
		sb.AppendLine("\t{");

		// TryGetName
		sb.AppendLine("\t\t/// <summary>Attempts to get the name of the enum value if it's a defined name.</summary>");
		sb.AppendLine("\t\t/// <param name=\"name\">When this method returns, contains the name of the enum value if it's a defined name; otherwise, <see langword=\"null\"/>.</param>");
		sb.AppendLine("\t\t/// <returns><see langword=\"true\"/> if the enum value has a defined name; otherwise, <see langword=\"false\"/>.</returns>");
		sb.AppendLine("\t\t[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
		sb.AppendLine("\t\tpublic bool TryGetName([global::System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? name)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tswitch (value)");
		sb.AppendLine("\t\t\t{");

		// De-dupe by numeric value (aliases). Pick the first name encountered.
		HashSet<Int64Value> seen = [];
		foreach (EnumMember m in model.Members)
		{
			if (!seen.Add(m.Value))
				continue;

			sb.Append("\t\t\t\tcase ").Append(type).Append('.').Append(m.Name)
			  .Append(": name = \"").Append(m.Name).AppendLine("\"; return true;");
		}

		sb.AppendLine("\t\t\t\tdefault: name = null; return false;");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		// ToStringFast
		sb.AppendLine("\t\t/// <summary>Returns the name of the enum value if it's a defined name; otherwise, falls back to the default <see cref=\"global::System.Enum.ToString()\"/> behavior.</summary>");
		sb.AppendLine("\t\t/// <returns>The name of the enum value if it's a defined name; otherwise, the result of <see cref=\"global::System.Enum.ToString()\"/>.</returns>");
		sb.AppendLine("\t\t[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
		sb.AppendLine("\t\tpublic string ToStringFast()");
		sb.AppendLine("\t\t\t=> value.TryGetName(out var name) ? name! : value.ToString();");
		sb.AppendLine();

		RenderTryFormat(sb, model, type, totalLength, useSpanUnsafeExtensions);

		// IsValidEnum
		if (model.IsFlags)
		{
			// Bare 0 passes only when a zero-valued member (e.g. None) is actually defined.
			bool zeroDefined = false;
			foreach (EnumMember zm in model.Members)
			{
				if (zm.Value.IsZero)
				{
					zeroDefined = true;
					break;
				}
			}

			sb.AppendLine("\t\t/// <summary>Indicates whether the given flags value is composed solely of defined flags.</summary>");
			sb.AppendLine("\t\t/// <returns><see langword=\"true\"/> if every set bit corresponds to a defined flag; otherwise, <see langword=\"false\"/>.</returns>");
			sb.AppendLine("\t\t[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
			sb.AppendLine("\t\tpublic bool IsValidEnum()");
			sb.Append("\t\t\t=> ");

			if (!zeroDefined)
			{
				sb.Append("value != 0 && ");
			}

			sb.AppendLine("(value & ~DEFINED_FLAGS_MASK) == 0;");
		}
		else
		{
			sb.AppendLine("\t\t/// <summary>Indicates whether the given enum value is a defined name.</summary>");
			sb.AppendLine("\t\t/// <returns><see langword=\"true\"/> if the value is a defined enum name; otherwise, <see langword=\"false\"/>.</returns>");
			sb.AppendLine("\t\t[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");

			if (TryGetContiguousRange(model.Members, out ulong min, out ulong max))
			{
				// The defined values are one unbroken run, so membership is a single unsigned compare.
				// Writing that as an expression rather than leaning on the TryGetName switch matters: the
				// switch reaches the same comparison but materializes the result through a branch, while
				// the expression form settles it with setbe.
				bool wide = "long".Equals(model.UnderlyingKeyword, StringComparison.Ordinal)
					||
					"ulong".Equals(model.UnderlyingKeyword, StringComparison.Ordinal);

				// A narrower cast than the underlying type would truncate an out-of-range value into the
				// valid window, so the comparison widens to match the storage.
				string cast = wide ? "ulong" : "uint";
				string suffix = wide ? "UL" : "u";

				sb.Append("\t\tpublic bool IsValidEnum() => ");

				if (min == 0)
				{
					sb.Append('(').Append(cast).Append(")value <= ").Append(max).Append(suffix).AppendLine(";");
				}
				else
				{
					// A run that starts above zero slides down to it first. The subtraction is explicitly
					// unchecked so a value below the run wraps to a large one and fails the compare, rather
					// than throwing if the consumer compiles with overflow checking on.
					sb.Append("unchecked((").Append(cast).Append(")value - ").Append(min).Append(suffix)
					  .Append(") <= ").Append(max - min).Append(suffix).AppendLine(";");
				}
			}
			else
			{
				// Sparse or negative values: the switch is left to pick its own search shape.
				sb.AppendLine("\t\tpublic bool IsValidEnum() => value.TryGetName(out _);");
			}
		}

		sb.AppendLine();

		// Values
		if (!usesNameTable)
		{
			// No name table is generated, so materialize the values inline as a read-only span literal.
			sb.AppendLine("\t\t/// <summary>Gets a read-only span of all enum values for this enum type.</summary>");
			sb.AppendLine("\t\t/// <value>A read-only span of all enum values.</value>");
			sb.Append("\t\tpublic static global::System.ReadOnlySpan<").Append(type).Append("> Values => [");

			for (int i = 0; i < model.Members.Length; i++)
			{
				if (i > 0)
				{
					sb.Append(", ");
				}

				sb.Append(type).Append('.').Append(model.Members[i].Name);
			}

			sb.AppendLine("];");
		}
		else
		{
			// The frozen name table already holds the values as an ImmutableArray, so surface that
			// directly (zero-copy on .NET 10) instead of re-listing every member - the dictionary
			// stays the single source of truth.
			sb.AppendLine("\t\t/// <summary>Gets an immutable array of all enum values for this enum type.</summary>");
			sb.AppendLine("\t\t/// <value>An immutable array containing every defined value of the enumeration.</value>");
			sb.AppendLine("#if NET10_0_OR_GREATER");
			sb.Append("\t\tpublic static global::System.Collections.Immutable.ImmutableArray<").Append(type).AppendLine("> Values => s_nameToValue.Values;");
			sb.AppendLine("#else");
			sb.Append("\t\tpublic static global::System.Collections.Immutable.ImmutableArray<").Append(type).AppendLine("> Values => global::System.Collections.Immutable.ImmutableArray.CreateRange(s_nameToValue.Values);");
			sb.AppendLine("#endif");
		}

		if (emitsParsing)
		{
			sb.AppendLine();

			if (usesNameTable)
			{
				RenderTableTryParse(sb, type);
			}
			else
			{
				RenderChainTryParse(sb, model, type);
			}

			RenderParseFast(sb, model, type);
		}

		sb.AppendLine("\t}");
		sb.AppendLine("}");

		return sb.ToString();
	}

	/// <summary>
	/// Emits the UTF-16 and UTF-8 <c>TryFormat</c> members inside the extension block.
	/// </summary>
	/// <remarks>
	/// Neither member is marked for aggressive inlining: the body grows with the member count, so forcing
	/// it into every call site works against the caller. The UTF-8 member is emitted only for .NET 8 and
	/// later, where the primitive <c>IUtf8SpanFormattable</c> fallback and <c>Encoding.TryGetBytes</c>
	/// both exist.
	/// </remarks>
	/// <param name="sb">The builder receiving the generated source.</param>
	/// <param name="model">The enum being rendered.</param>
	/// <param name="type">The fully qualified name of the enum.</param>
	/// <param name="totalLength">The maximum formatted length of the enum's names.</param>
	/// <param name="useSpanUnsafeExtensions">A value that indicates whether the consuming compilation can reach <c>SpanUnsafeExtensions</c>.</param>
	private static void RenderTryFormat(StringBuilder sb, EnumModel model, string type, int totalLength, bool useSpanUnsafeExtensions)
	{
		// A value with no defined name falls back to its numeric form, which for a 64-bit underlying type
		// runs to 20 characters - wider than the names of a small enum, so the scratch buffer used by the
		// transcoding path takes the larger of the two.
		int fallbackLength = Math.Max(totalLength, 20);

		sb.AppendLine("\t\t/// <summary>Attempts to write this value's defined name into a character buffer.</summary>");
		sb.AppendLine("\t\t/// <remarks>Falls back to the value's numeric or flags representation when it has no defined name.</remarks>");
		sb.AppendLine("\t\t/// <param name=\"destination\">The buffer that receives the formatted value.</param>");
		sb.AppendLine("\t\t/// <param name=\"charsWritten\">When this method returns, contains the number of characters written to <paramref name=\"destination\"/>.</param>");
		sb.AppendLine("\t\t/// <returns><see langword=\"true\"/> if the value was written; otherwise, <see langword=\"false\"/> when <paramref name=\"destination\"/> is too short.</returns>");
		sb.AppendLine("\t\tpublic bool TryFormat(global::System.Span<char> destination, out int charsWritten)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tif (value.TryGetName(out string? name))");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\tif (name.Length <= destination.Length)");
		sb.AppendLine("\t\t\t\t{");
		RenderNameCopy(sb, "\t\t\t\t\t", "global::System.MemoryExtensions.AsSpan(name)", "destination", useSpanUnsafeExtensions);
		sb.AppendLine("\t\t\t\t\tcharsWritten = name.Length;");
		sb.AppendLine("\t\t\t\t\treturn true;");
		sb.AppendLine("\t\t\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t\t\tcharsWritten = 0;");
		sb.AppendLine("\t\t\t\treturn false;");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine();

		if (model.IsFlags)
		{
			// Composing the comma-joined listing for a combined value is not worth generating.
			sb.AppendLine("\t\t\treturn global::System.Enum.TryFormat(value, destination, out charsWritten);");
		}
		else
		{
			sb.Append("\t\t\treturn ((").Append(model.UnderlyingKeyword).AppendLine(")value).TryFormat(destination, out charsWritten);");
		}

		sb.AppendLine("\t\t}");
		sb.AppendLine();

		sb.AppendLine("#if NET8_0_OR_GREATER");
		sb.AppendLine("\t\t/// <summary>Attempts to write this value's defined name into a UTF-8 buffer.</summary>");
		sb.AppendLine("\t\t/// <remarks>Falls back to the value's numeric or flags representation when it has no defined name.</remarks>");
		sb.AppendLine("\t\t/// <param name=\"utf8Destination\">The buffer that receives the formatted value.</param>");
		sb.AppendLine("\t\t/// <param name=\"bytesWritten\">When this method returns, contains the number of bytes written to <paramref name=\"utf8Destination\"/>.</param>");
		sb.AppendLine("\t\t/// <returns><see langword=\"true\"/> if the value was written; otherwise, <see langword=\"false\"/> when <paramref name=\"utf8Destination\"/> is too short.</returns>");
		sb.AppendLine("\t\tpublic bool TryFormat(global::System.Span<byte> utf8Destination, out int bytesWritten)");
		sb.AppendLine("\t\t{");

		if (model.AllNamesAscii)
		{
			// Every name is ASCII, so each one becomes a compile-time UTF-8 blob and the write is a plain
			// copy - no transcoding, and no character buffer in between.
			sb.AppendLine("\t\t\tglobal::System.ReadOnlySpan<byte> name;");
			sb.AppendLine("\t\t\tswitch (value)");
			sb.AppendLine("\t\t\t{");

			HashSet<Int64Value> seen = [];
			foreach (EnumMember m in model.Members)
			{
				if (!seen.Add(m.Value))
					continue;

				sb.Append("\t\t\t\tcase ").Append(type).Append('.').Append(m.Name)
				  .Append(": name = \"").Append(m.Name).AppendLine("\"u8; break;");
			}

			sb.AppendLine("\t\t\t\tdefault:");
			RenderUtf8Fallback(sb, model, fallbackLength, "\t\t\t\t\t", useSpanUnsafeExtensions);
			sb.AppendLine("\t\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\t\tif (name.Length <= utf8Destination.Length)");
			sb.AppendLine("\t\t\t{");
			RenderNameCopy(sb, "\t\t\t\t", "name", "utf8Destination", useSpanUnsafeExtensions);
			sb.AppendLine("\t\t\t\tbytesWritten = name.Length;");
			sb.AppendLine("\t\t\t\treturn true;");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\t\tbytesWritten = 0;");
			sb.AppendLine("\t\t\treturn false;");
		}
		else
		{
			// At least one name carries a non-ASCII character, so there is no compile-time UTF-8 blob to
			// copy - format to characters and transcode instead.
			RenderUtf8Fallback(sb, model, fallbackLength, "\t\t\t", useSpanUnsafeExtensions);
		}

		sb.AppendLine("\t\t}");
		sb.AppendLine("#endif");
		sb.AppendLine();
	}

	/// <summary>
	/// Emits the UTF-8 path taken by a value that has no defined name.
	/// </summary>
	/// <param name="sb">The builder receiving the generated source.</param>
	/// <param name="model">The enum being rendered.</param>
	/// <param name="fallbackLength">The length of the scratch character buffer used when transcoding.</param>
	/// <param name="indent">The leading tabs to place before each emitted line.</param>
	/// <param name="useSpanUnsafeExtensions">A value that indicates whether the consuming compilation can reach <c>SpanUnsafeExtensions</c>.</param>
	private static void RenderUtf8Fallback(StringBuilder sb, EnumModel model, int fallbackLength, string indent, bool useSpanUnsafeExtensions)
	{
		if (!model.IsFlags)
		{
			// A single undefined value formats as its underlying number, which writes UTF-8 directly.
			sb.Append(indent).Append("return ((").Append(model.UnderlyingKeyword).AppendLine(")value).TryFormat(utf8Destination, out bytesWritten);");
			return;
		}

		// A combined flags value formats as a comma-joined listing, which only Enum composes - reach it
		// through the character overload above and transcode the result.
		sb.Append(indent).Append("global::System.Span<char> chars = stackalloc char[").Append(fallbackLength).AppendLine("];");
		sb.Append(indent).AppendLine("if (!value.TryFormat(chars, out int written))");
		sb.Append(indent).AppendLine("{");
		sb.Append(indent).AppendLine("\tbytesWritten = 0;");
		sb.Append(indent).AppendLine("\treturn false;");
		sb.Append(indent).AppendLine("}");
		sb.AppendLine();
		sb.Append(indent).Append("return global::System.Text.Encoding.UTF8.TryGetBytes(");

		// The character overload only reports success after writing inside the buffer, so 'written' is already
		// known to be in range and the slice's own check has nothing left to decide.
		if (useSpanUnsafeExtensions)
		{
			sb.Append(SPAN_UNSAFE_EXTENSIONS).Append(".UnsafeSlice(chars, 0, written)");
		}
		else
		{
			sb.Append("chars.Slice(0, written)");
		}

		sb.AppendLine(", utf8Destination, out bytesWritten);");
	}

	/// <summary>
	/// Emits the copy of a matched name into the caller's buffer.
	/// </summary>
	/// <remarks>
	/// Both call sites stand behind a length guard that is exactly <c>UnsafeCopy</c>'s precondition, so where the
	/// helper is available the copy's own bounds check is settled work repeated at runtime. The <c>CopyTo</c>
	/// fallback keeps the generated source valid in a compilation that cannot reach the helper.
	/// </remarks>
	/// <param name="sb">The builder receiving the generated source.</param>
	/// <param name="indent">The leading tabs to place before the emitted line.</param>
	/// <param name="source">The expression naming the read-only span that holds the name.</param>
	/// <param name="destination">The expression naming the caller's buffer.</param>
	/// <param name="useSpanUnsafeExtensions">A value that indicates whether the consuming compilation can reach <c>SpanUnsafeExtensions</c>.</param>
	private static void RenderNameCopy(StringBuilder sb, string indent, string source, string destination, bool useSpanUnsafeExtensions)
	{
		sb.Append(indent);

		if (useSpanUnsafeExtensions)
		{
			sb.Append(SPAN_UNSAFE_EXTENSIONS).Append(".UnsafeCopy(").Append(source).Append(", ").Append(destination).AppendLine(");");
		}
		else
		{
			sb.Append(source).Append(".CopyTo(").Append(destination).AppendLine(");");
		}
	}

	/// <summary>
	/// Emits the class-scope name table, and its span alternate lookup, backing the parsing members.
	/// </summary>
	/// <param name="sb">The builder receiving the generated source.</param>
	/// <param name="model">The enum being rendered.</param>
	/// <param name="type">The fully qualified name of the enum.</param>
	private static void RenderNameTable(StringBuilder sb, EnumModel model, string type)
	{
		sb.AppendLine("\t// String -> Enum lookup (name -> value)");
		sb.AppendLine("#if NET10_0_OR_GREATER");

		sb.Append("\tprivate static readonly global::System.Collections.Frozen.FrozenDictionary<string, ")
		  .Append(type)
		  .AppendLine("> s_nameToValue =")
		  .Append("\t\tglobal::System.Collections.Frozen.FrozenDictionary.Create<string, ")
		  .Append(type)
		  .AppendLine(">(global::System.StringComparer.OrdinalIgnoreCase, ");

		sb.Append("\t\t\tnew global::System.Collections.Generic.KeyValuePair<string, ")
		  .Append(type)
		  .Append(">(\"")
		  .Append(model.Members[0].Name)
		  .Append("\", ")
		  .Append(type).Append('.').Append(model.Members[0].Name)
		  .Append(')');

		foreach (EnumMember m in model.Members.AsSpan(1, model.Members.Length - 1))
		{
			// include aliases too (same numeric value but different name)
			sb.Append(',').AppendLine();
			sb.Append("\t\t\tnew global::System.Collections.Generic.KeyValuePair<string, ")
			  .Append(type)
			  .Append(">(\"")
			  .Append(m.Name)
			  .Append("\", ")
			  .Append(type).Append('.').Append(m.Name)
			  .Append(')');
		}

		sb.AppendLine(");");
		sb.AppendLine("#else");

		sb.Append("\tprivate static readonly global::System.Collections.Generic.Dictionary<string, ")
		  .Append(type)
		  .AppendLine("> s_nameToValue = new(global::System.StringComparer.OrdinalIgnoreCase)")
		  .AppendLine("\t{");

		foreach (EnumMember m in model.Members)
		{
			sb.Append("\t\t[\"")
			  .Append(m.Name)
			  .Append("\"] = ")
			  .Append(type).Append('.').Append(m.Name)
			  .AppendLine(",");
		}

		sb.AppendLine("\t};");
		sb.AppendLine("#endif");
		sb.AppendLine();

		sb.AppendLine("\t// Span<char> alternate lookup over the name table (allocation-free name parsing on .NET 9+).");
		sb.AppendLine("#if NET10_0_OR_GREATER");
		sb.Append("\tprivate static readonly global::System.Collections.Frozen.FrozenDictionary<string, ").Append(type).AppendLine(">.AlternateLookup<global::System.ReadOnlySpan<char>> s_nameSpanLookup = s_nameToValue.GetAlternateLookup<global::System.ReadOnlySpan<char>>();");
		sb.AppendLine("#elif NET9_0_OR_GREATER");
		sb.Append("\tprivate static readonly global::System.Collections.Generic.Dictionary<string, ").Append(type).AppendLine(">.AlternateLookup<global::System.ReadOnlySpan<char>> s_nameSpanLookup = s_nameToValue.GetAlternateLookup<global::System.ReadOnlySpan<char>>();");
		sb.AppendLine("#endif");
		sb.AppendLine();
	}


	/// <summary>
	/// Emits the two <c>TryParseFast</c> overloads backed by the frozen name table.
	/// </summary>
	/// <remarks>
	/// The out parameter is named <c>result</c> rather than <c>value</c> because a member parameter may
	/// not shadow the extension receiver (CS9290).
	/// </remarks>
	/// <param name="sb">The builder receiving the generated source.</param>
	/// <param name="type">The fully qualified name of the enum.</param>
	private static void RenderTableTryParse(StringBuilder sb, string type)
	{
		sb.AppendLine("\t\t/// <summary>Attempts to parse the string representation of the enum ignoring case.</summary>");
		sb.AppendLine("\t\t/// <param name=\"text\">The string representation of the enum.</param>");
		sb.AppendLine("\t\t/// <param name=\"result\">When this method returns, contains the enum value equivalent to the string, if the conversion succeeded, or the default value if the conversion failed.</param>");
		sb.AppendLine("\t\t/// <returns><see langword=\"true\"/> if the string was converted successfully; otherwise, <see langword=\"false\"/>.</returns>");
		sb.AppendLine("\t\t[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
		sb.Append("\t\tpublic static bool TryParseFast([global::System.Diagnostics.CodeAnalysis.NotNullWhen(true)] string? text, out ").Append(type).AppendLine(" result)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tif (text is null)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\tresult = default;");
		sb.AppendLine("\t\t\t\treturn false;");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t\tif (s_nameToValue.TryGetValue(text, out result))");
		sb.AppendLine("\t\t\t\treturn true;");
		sb.AppendLine();
		sb.AppendLine("\t\t\t// Fallback: supports numeric text and Flags combinations (and anything else ToString() might produce).");
		sb.AppendLine("\t\t\treturn global::System.Enum.TryParse(text, ignoreCase: true, out result);");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		// Span<char> overload - parse a name from a character span without allocating a string (.NET 9+).
		sb.AppendLine("\t\t/// <summary>Attempts to parse the character span representation of the enum ignoring case.</summary>");
		sb.AppendLine("\t\t/// <param name=\"text\">The character span representation of the enum.</param>");
		sb.AppendLine("\t\t/// <param name=\"result\">When this method returns, contains the enum value equivalent to the span, if the conversion succeeded, or the default value if the conversion failed.</param>");
		sb.AppendLine("\t\t/// <returns><see langword=\"true\"/> if the span was converted successfully; otherwise, <see langword=\"false\"/>.</returns>");
		sb.AppendLine("\t\t[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
		sb.Append("\t\tpublic static bool TryParseFast(global::System.ReadOnlySpan<char> text, out ").Append(type).AppendLine(" result)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("#if NET9_0_OR_GREATER");
		sb.AppendLine("\t\t\tif (s_nameSpanLookup.TryGetValue(text, out result))");
		sb.AppendLine("\t\t\t\treturn true;");
		sb.AppendLine();
		sb.AppendLine("\t\t\t// Fallback: supports numeric text and Flags combinations (and anything else ToString() might produce).");
		sb.AppendLine("\t\t\treturn global::System.Enum.TryParse(text, ignoreCase: true, out result);");
		sb.AppendLine("#else");
		sb.AppendLine("\t\t\tif (text.IsEmpty)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\tresult = default;");
		sb.AppendLine("\t\t\t\treturn false;");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t\t// Older frameworks lack a span alternate lookup; fall back to the string overload.");
		sb.AppendLine("\t\t\treturn TryParseFast(text.ToString(), out result);");
		sb.AppendLine("#endif");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Emits the two <c>TryParseFast</c> overloads as a length-bucketed comparison chain.
	/// </summary>
	/// <remarks>
	/// <para>This is the shape used when the name set is small enough that a frozen table costs more than
	/// it saves. <c>FrozenDictionary</c> resolves such a set to a <c>LengthBucketsFrozenDictionary</c>,
	/// which buckets by length and then scans the bucket with the same ordinal comparison emitted here -
	/// so the chain does equivalent work while skipping the table's construction and retained storage.</para>
	/// <para>The span overload carries the chain and the string overload forwards to it, which also drops
	/// the span alternate lookup and its framework conditionals.</para>
	/// </remarks>
	/// <param name="sb">The builder receiving the generated source.</param>
	/// <param name="model">The enum being rendered.</param>
	/// <param name="type">The fully qualified name of the enum.</param>
	private static void RenderChainTryParse(StringBuilder sb, EnumModel model, string type)
	{
		sb.AppendLine("\t\t/// <summary>Attempts to parse the string representation of the enum ignoring case.</summary>");
		sb.AppendLine("\t\t/// <param name=\"text\">The string representation of the enum.</param>");
		sb.AppendLine("\t\t/// <param name=\"result\">When this method returns, contains the enum value equivalent to the string, if the conversion succeeded, or the default value if the conversion failed.</param>");
		sb.AppendLine("\t\t/// <returns><see langword=\"true\"/> if the string was converted successfully; otherwise, <see langword=\"false\"/>.</returns>");
		sb.AppendLine("\t\t[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
		sb.Append("\t\tpublic static bool TryParseFast([global::System.Diagnostics.CodeAnalysis.NotNullWhen(true)] string? text, out ").Append(type).AppendLine(" result)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tif (text is null)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\tresult = default;");
		sb.AppendLine("\t\t\t\treturn false;");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t\treturn TryParseFast(global::System.MemoryExtensions.AsSpan(text), out result);");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		sb.AppendLine("\t\t/// <summary>Attempts to parse the character span representation of the enum ignoring case.</summary>");
		sb.AppendLine("\t\t/// <param name=\"text\">The character span representation of the enum.</param>");
		sb.AppendLine("\t\t/// <param name=\"result\">When this method returns, contains the enum value equivalent to the span, if the conversion succeeded, or the default value if the conversion failed.</param>");
		sb.AppendLine("\t\t/// <returns><see langword=\"true\"/> if the span was converted successfully; otherwise, <see langword=\"false\"/>.</returns>");

		// Deliberately not inlined: the chain grows with the name count, and each arm is itself a call.
		sb.Append("\t\tpublic static bool TryParseFast(global::System.ReadOnlySpan<char> text, out ").Append(type).AppendLine(" result)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tswitch (text.Length)");
		sb.AppendLine("\t\t\t{");

		// Group the names by length so a wrong-length input is rejected by the switch alone. Lengths are
		// emitted in ascending order to keep the output stable across runs for the incremental cache.
		Dictionary<int, List<EnumMember>> byLength = [];
		foreach (EnumMember member in model.Members)
		{
			if (!byLength.TryGetValue(member.Name.Length, out List<EnumMember>? bucket))
			{
				bucket = [];
				byLength[member.Name.Length] = bucket;
			}

			bucket.Add(member);
		}

		List<int> lengths = new(byLength.Keys);
		lengths.Sort();

		foreach (int length in lengths)
		{
			sb.Append("\t\t\t\tcase ").Append(length).AppendLine(":");

			foreach (EnumMember member in byLength[length])
			{
				// Called statically: the generated file carries no usings, so the MemoryExtensions
				// extension form would only bind when the consumer happens to enable implicit usings.
				sb.Append("\t\t\t\t\tif (global::System.MemoryExtensions.Equals(text, \"").Append(member.Name)
				  .Append("\", global::System.StringComparison.OrdinalIgnoreCase)) { result = ")
				  .Append(type).Append('.').Append(member.Name).AppendLine("; return true; }");
			}

			sb.AppendLine("\t\t\t\t\tbreak;");
		}

		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t\t// Fallback: supports numeric text and Flags combinations (and anything else ToString() might produce).");
		sb.AppendLine("\t\t\treturn global::System.Enum.TryParse(text, ignoreCase: true, out result);");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Emits the two throwing <c>ParseFast</c> overloads, which are identical under either parse strategy.
	/// </summary>
	/// <param name="sb">The builder receiving the generated source.</param>
	/// <param name="model">The enum being rendered.</param>
	/// <param name="type">The fully qualified name of the enum.</param>
	private static void RenderParseFast(StringBuilder sb, EnumModel model, string type)
	{
		sb.AppendLine("\t\t/// <summary>Parses the string representation of the enum ignoring case.</summary>");
		sb.AppendLine("\t\t/// <param name=\"text\">The string representation of the enum.</param>");
		sb.AppendLine("\t\t/// <returns>The parsed enum value.</returns>");
		sb.AppendLine("\t\t/// <exception cref=\"global::System.FormatException\">Thrown when the input string does not correspond to a valid enum name or value.</exception>");
		sb.AppendLine("\t\t[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
		sb.Append("\t\tpublic static ").Append(type).AppendLine(" ParseFast(string text)");
		sb.AppendLine("\t\t\t=> TryParseFast(text, out var result)");
		sb.AppendLine("\t\t\t\t? result");
		sb.Append("\t\t\t\t: throw new global::System.FormatException(\"Unrecognized value for enum ").Append(model.EnumName).AppendLine(": '\" + text + \"'.\");");
		sb.AppendLine();

		sb.AppendLine("\t\t/// <summary>Parses the character span representation of the enum ignoring case.</summary>");
		sb.AppendLine("\t\t/// <param name=\"text\">The character span representation of the enum.</param>");
		sb.AppendLine("\t\t/// <returns>The parsed enum value.</returns>");
		sb.AppendLine("\t\t/// <exception cref=\"global::System.FormatException\">Thrown when the input span does not correspond to a valid enum name or value.</exception>");
		sb.AppendLine("\t\t[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
		sb.Append("\t\tpublic static ").Append(type).AppendLine(" ParseFast(global::System.ReadOnlySpan<char> text)");
		sb.AppendLine("\t\t\t=> TryParseFast(text, out var result)");
		sb.AppendLine("\t\t\t\t? result");
		sb.Append("\t\t\t\t: throw new global::System.FormatException(\"Unrecognized value for enum ").Append(model.EnumName).AppendLine(": '\" + text.ToString() + \"'.\");");
	}

	/// <summary>Builds a unique, stable file hint name from the enum's fully qualified name.</summary>
	/// <param name="enumSymbol">The attributed enum.</param>
	/// <returns>The hint name, carrying no characters that are illegal in a file name.</returns>
	private static string MakeHintName(INamedTypeSymbol enumSymbol)
	{
		// Unique, stable-ish hint name.
		string fq = enumSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		return fq.Replace("global::", "")
				 .Replace('<', '_')
				 .Replace('>', '_')
				 .Replace('.', '_')
				 .Replace('+', '_');
	}

	private const string ATTRIBUTE_SOURCE = """
// <auto-generated/>
#nullable enable
namespace MG.Sonarr.Next.Generators
{
	/// <summary>
	/// Marks an enum as the source for generated conversion, validation, and parsing extension members.
	/// </summary>
	/// <remarks>
	/// Applying this attribute to an enum named <c>Foo</c> generates a static <c>FooExtensions</c> class in the enum's
	/// namespace, with the enum's accessibility.
	/// </remarks>
	[global::System.AttributeUsage(global::System.AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
	internal sealed class GenerateEnumToStringAttribute : global::System.Attribute
	{
		/// <summary>Gets or sets a value that indicates whether the generated class leaves out its parsing members.</summary>
		/// <value>
		/// <see langword="true"/> to generate no <c>TryParseFast</c> or <c>ParseFast</c> members; otherwise,
		/// <see langword="false"/>. The default is <see langword="false"/>.
		/// </value>
		public bool DisableStringToEnumDictionary { get; set; }
	}
}
""";

	/// <summary>Describes one attributed enum, as plain values that the incremental cache can compare.</summary>
	/// <remarks>
	/// Record value equality lets the incremental pipeline skip rendering an enum that an edit leaves unchanged.
	/// </remarks>
	/// <param name="Namespace">The enum's namespace, or <see langword="null"/> for the global namespace.</param>
	/// <param name="EnumName">The enum's simple name.</param>
	/// <param name="EnumFullType">The enum's fully qualified name, with its <c>global::</c> alias.</param>
	/// <param name="UnderlyingKeyword">The C# keyword naming the enum's underlying type.</param>
	/// <param name="AllNamesAscii">A value that indicates whether every member name is ASCII.</param>
	/// <param name="IsFlags">A value that indicates whether the enum carries <see cref="FlagsAttribute"/>.</param>
	/// <param name="DisableDictionary">A value that indicates whether the attribute turns the parsing members off.</param>
	/// <param name="Accessibility">The enum's declared accessibility, which the extensions class copies.</param>
	/// <param name="Members">The enum's defined members.</param>
	/// <param name="HintName">The file hint name for the enum's generated source.</param>
	private sealed record EnumModel(
		string? Namespace,
		string EnumName,
		string EnumFullType,
		string UnderlyingKeyword,
		bool AllNamesAscii,
		bool IsFlags,
		bool DisableDictionary,
		Accessibility Accessibility,
		EnumMember[] Members,
		string HintName);

	/// <summary>
	/// Maps an enum's underlying type onto the C# keyword naming it.
	/// </summary>
	/// <param name="underlyingType">The enum's underlying integral type.</param>
	/// <returns>The keyword for <paramref name="underlyingType"/>, defaulting to <c>int</c>.</returns>
	private static string GetUnderlyingKeyword(INamedTypeSymbol? underlyingType)
	{
		switch (underlyingType?.SpecialType)
		{
			case SpecialType.System_Byte: return "byte";
			case SpecialType.System_SByte: return "sbyte";
			case SpecialType.System_Int16: return "short";
			case SpecialType.System_UInt16: return "ushort";
			case SpecialType.System_UInt32: return "uint";
			case SpecialType.System_Int64: return "long";
			case SpecialType.System_UInt64: return "ulong";
			default: return "int";
		}
	}

	/// <summary>
	/// Determines whether the distinct member values run contiguously, from any starting point.
	/// </summary>
	/// <remarks>
	/// Aliases are folded out first, so a value repeated under a second name does not disturb the run.
	/// A single negative member disqualifies the enum, because the unsigned comparison this enables would
	/// read that member as an enormous positive value.
	/// </remarks>
	/// <param name="members">The enum's members.</param>
	/// <param name="min">When this method returns, contains the lowest defined value; otherwise, zero.</param>
	/// <param name="max">When this method returns, contains the highest defined value; otherwise, zero.</param>
	/// <returns><see langword="true"/> when the distinct values are exactly <paramref name="min"/> through <paramref name="max"/>; otherwise, <see langword="false"/>.</returns>
	private static bool TryGetContiguousRange(EnumMember[] members, out ulong min, out ulong max)
	{
		min = 0;
		max = 0;
		HashSet<Int64Value> distinct = [];
		ulong lowest = ulong.MaxValue;
		ulong highest = 0;

		foreach (EnumMember member in members)
		{
			if (!distinct.Add(member.Value))
				continue;

			ulong current;
			if (member.Value.TryGetLong(out long signed))
			{
				if (signed < 0)
					return false;

				current = unchecked((ulong)signed);
			}
			else
			{
				current = member.Value.AsUInt64;
			}

			if (current > highest)
			{
				highest = current;
			}

			if (current < lowest)
			{
				lowest = current;
			}
		}

		// N distinct non-negative values spanning exactly N-1 can only be the full run min..max, so the
		// count and the two endpoints together rule out any gap without a second pass.
		if (distinct.Count == 0 || highest - lowest != unchecked((ulong)(distinct.Count - 1)))
			return false;

		min = lowest;
		max = highest;
		return true;
	}

	/// <summary>
	/// Indicates whether every member name is ASCII, and so can be emitted as a UTF-8 string literal.
	/// </summary>
	/// <param name="members">The enum's members.</param>
	/// <returns><see langword="true"/> when no name carries a character above U+007F; otherwise, <see langword="false"/>.</returns>
	private static bool AllNamesAreAscii(EnumMember[] members)
	{
		foreach (EnumMember member in members)
		{
			foreach (char c in member.Name)
			{
				if (c > 0x7F)
				{
					return false;
				}
			}
		}

		return true;
	}

	/// <summary>Pairs an enum member's name with its value.</summary>
	/// <param name="Name">The member's name.</param>
	/// <param name="Value">The member's value, widened to 64 bits with its signedness kept.</param>
	private sealed record EnumMember(string Name, Int64Value Value);

	/// <summary>Collects the enum's defined members along with their values.</summary>
	/// <remarks>
	/// A constant that overflows <see cref="long"/> can only belong to an enum whose underlying type is <see cref="ulong"/>,
	/// so it is read as unsigned instead.
	/// </remarks>
	/// <param name="enumSymbol">The enum to read.</param>
	/// <returns>The defined members, or an empty array when the enum declares none.</returns>
	private static EnumMember[] GetMembers(INamedTypeSymbol enumSymbol)
	{
		ReadOnlySpan<ISymbol> members = enumSymbol.GetMembers().AsSpan();
		EnumMember[]? array = null;

		int count = 0;
		foreach (var member in members)
		{
			if (member is not IFieldSymbol field
				|| !field.HasConstantValue
				|| "value__".Equals(field.Name, StringComparison.Ordinal))
			{
				continue;
			}

			array ??= new EnumMember[members.Length];
			try
			{
				long value = Convert.ToInt64(field.ConstantValue!);
				array[count++] = new EnumMember(field.Name, value);
			}
			catch (OverflowException)
			{
				ulong value = Convert.ToUInt64(field.ConstantValue!);
				array[count++] = new EnumMember(field.Name, value);
			}
		}

		if (array is null || count == 0) return [];

		if (count < array.Length)
		{
			Array.Resize(ref array, count);
		}

		return array;
	}

	/// <summary>
	/// Represents an enum member's value widened to 64 bits, along with whether it came from a signed or an unsigned type.
	/// </summary>
	/// <remarks>
	/// Two values are equal only when both their bits and their kind match, so a <see cref="long"/> and a
	/// <see cref="ulong"/> that share a bit pattern stay distinct.
	/// </remarks>
	[StructLayout(LayoutKind.Auto)]
	private readonly struct Int64Value : IEquatable<Int64Value>
	{
		private readonly ulong _value;
		private readonly byte _kind; // 0 = long, 1 = ulong

		/// <summary>Initializes a new <see cref="Int64Value"/> instance with the specified signed value.</summary>
		/// <param name="value">The signed value to store.</param>
		public Int64Value(long value)
		{
			_value = unchecked((ulong)value);
			_kind = 0;
		}

		/// <summary>Initializes a new <see cref="Int64Value"/> instance with the specified unsigned value.</summary>
		/// <param name="value">The unsigned value to store.</param>
		public Int64Value(ulong value)
		{
			_value = value;
			_kind = 1;
		}

		/// <summary>Gets a value that indicates whether the value came from a signed type.</summary>
		/// <value><see langword="true"/> if the value was created from a <see cref="long"/>; otherwise, <see langword="false"/>.</value>
		public bool IsLong
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _kind == 0;
		}

		/// <summary>Gets a value that indicates whether the value came from an unsigned type.</summary>
		/// <value><see langword="true"/> if the value was created from a <see cref="ulong"/>; otherwise, <see langword="false"/>.</value>
		public bool IsUInt64
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _kind == 1;
		}

		/// <summary>Gets a value that indicates whether the value is zero.</summary>
		/// <value><see langword="true"/> if no bit of the value is set; otherwise, <see langword="false"/>.</value>
		public bool IsZero
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _value == 0;
		}

		/// <summary>Gets the value as a signed integer.</summary>
		/// <exception cref="InvalidOperationException">Thrown when the value was created from a ulong.</exception>
		/// <value>The stored value, read as signed.</value>
		public long AsLong
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _kind == 0
				? unchecked((long)_value)
				: throw new InvalidOperationException("The stored value was created from a ulong.");
		}

		/// <summary>Gets the value as an unsigned integer.</summary>
		/// <exception cref="InvalidOperationException">Thrown when the value was created from a long.</exception>
		/// <value>The stored value, read as unsigned.</value>
		public ulong AsUInt64
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _kind == 1
				? _value
				: throw new InvalidOperationException("The stored value was created from a long.");
		}

		/// <summary>Attempts to get the value as a signed integer.</summary>
		/// <param name="value">When this method returns, contains the stored value if it came from a signed type; otherwise, zero.</param>
		/// <returns><see langword="true"/> if the value was created from a <see cref="long"/>; otherwise, <see langword="false"/>.</returns>
		public bool TryGetLong(out long value)
		{
			if (_kind == 0)
			{
				value = unchecked((long)_value);
				return true;
			}

			value = default;
			return false;
		}

		/// <summary>Attempts to get the value as an unsigned integer.</summary>
		/// <param name="value">When this method returns, contains the stored value if it came from an unsigned type; otherwise, zero.</param>
		/// <returns><see langword="true"/> if the value was created from a <see cref="ulong"/>; otherwise, <see langword="false"/>.</returns>
		public bool TryGetUInt64(out ulong value)
		{
			if (_kind == 1)
			{
				value = _value;
				return true;
			}

			value = default;
			return false;
		}

		/// <summary>Returns the value in decimal, read with the signedness it was created with.</summary>
		/// <returns>The value's decimal representation.</returns>
		public override string ToString()
		{
			return _kind == 0
					? unchecked((long)_value).ToString()
					: _value.ToString();
		}

		/// <summary>Determines whether this value equals another, comparing both bits and kind.</summary>
		/// <param name="other">The value to compare with.</param>
		/// <returns><see langword="true"/> if both the bits and the kind match; otherwise, <see langword="false"/>.</returns>
		public bool Equals(Int64Value other)
		{
			return _value == other._value && _kind == other._kind;
		}

		/// <summary>Determines whether this value equals an object, which may be another <see cref="Int64Value"/> or a boxed integer.</summary>
		/// <remarks>
		/// A boxed integer is wrapped before the comparison, so its kind has to match too: every integral type except
		/// <see cref="ulong"/> wraps as signed.
		/// </remarks>
		/// <param name="obj">The object to compare with.</param>
		/// <returns><see langword="true"/> if <paramref name="obj"/> wraps to an equal value; otherwise, <see langword="false"/>.</returns>
		public override bool Equals(object obj)
		{
			return obj switch
			{
				Int64Value full => this.Equals(full),
				byte b => this.Equals(new Int64Value(b)),
				sbyte sb => this.Equals(new Int64Value(sb)),
				short s => this.Equals(new Int64Value(s)),
				ushort us => this.Equals(new Int64Value(us)),
				int i => this.Equals(new Int64Value(i)),
				uint ui => this.Equals(new Int64Value(ui)),
				long l => this.Equals(new Int64Value(l)),
				ulong ul => this.Equals(new Int64Value(ul)),
				_ => false
			};
		}

		/// <summary>Returns a hash code for the value.</summary>
		/// <returns>The hash code.</returns>
		public override int GetHashCode()
		{
			return _kind == 0
				? unchecked((long)_value).GetHashCode()
				: _value.GetHashCode();
		}

		/// <summary>Converts a signed value to an <see cref="Int64Value"/>.</summary>
		/// <param name="value">The signed value to wrap.</param>
		/// <returns>The wrapped value.</returns>
		public static implicit operator Int64Value(long value) => new(value);

		/// <summary>Converts an unsigned value to an <see cref="Int64Value"/>.</summary>
		/// <param name="value">The unsigned value to wrap.</param>
		/// <returns>The wrapped value.</returns>
		public static implicit operator Int64Value(ulong value) => new(value);
	}
}
