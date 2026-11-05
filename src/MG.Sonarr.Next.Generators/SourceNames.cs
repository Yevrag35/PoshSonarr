using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;

namespace MG.Sonarr.Next.Generators;

/// <summary>
/// Provides the names that the generators write into generated source and pass as its hint names.
/// </summary>
internal static class SourceNames
{
	private const string GLOBAL_ALIAS = "global::";

	/// <summary>Appends the names of the types that contain a type, outermost first, with nothing between them.</summary>
	/// <remarks>
	/// A generated class sits at namespace scope, so a nested type's class folds its containing types' names into its
	/// own to keep two like-named nested types apart.
	/// </remarks>
	/// <param name="sb">The builder to append to.</param>
	/// <param name="containing">The type that contains the nested type, or <see langword="null"/> for a top-level type.</param>
	public static void AppendContainingTypeNames(StringBuilder sb, INamedTypeSymbol? containing)
	{
		if (containing is null)
			return;

		AppendContainingTypeNames(sb, containing.ContainingType);
		sb.Append(containing.Name);
	}

	/// <summary>Escapes an identifier with <c>@</c> when it's a reserved C# keyword.</summary>
	/// <remarks>
	/// A symbol's name holds the identifier without the <c>@</c> that its source wrote, so a member named
	/// <c>@class</c> has the name <c>class</c>, which generated code has to escape again. A contextual keyword, such as
	/// <c>value</c>, is already an identifier and stays as it is.
	/// </remarks>
	/// <param name="identifier">The identifier to escape.</param>
	/// <returns>The identifier, with an <c>@</c> in front of it when it's a reserved keyword.</returns>
	public static string EscapeIdentifier(string identifier)
	{
		return SyntaxFacts.GetKeywordKind(identifier) == SyntaxKind.None
			? identifier
			: "@" + identifier;
	}

	/// <summary>Gets the namespace that holds a symbol, as a namespace declaration writes it.</summary>
	/// <param name="symbol">The symbol whose namespace to get.</param>
	/// <returns>
	/// The namespace's name, with each keyword in it escaped, or <see langword="null"/> when the symbol is in the global
	/// namespace.
	/// </returns>
	public static string? GetNamespace(ISymbol symbol)
	{
		INamespaceSymbol? ns = symbol.ContainingNamespace;
		return ns is null || ns.IsGlobalNamespace
			? null
			: StripGlobalAlias(ns.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
	}

	/// <summary>Builds a file hint name from a fully qualified type name.</summary>
	/// <remarks>
	/// <para>
	/// The dots stay, so the name is as unique as the type's: <c>A.B_C</c> and <c>A_B.C</c> keep distinct names, where
	/// two equal hint names would make the generator throw and drop all of its output. A type and a namespace of the
	/// same name can't share a parent, so no two types in one compilation give the same name.
	/// </para>
	/// <para>
	/// The compiler takes only letters, digits, and a few punctuation characters in a hint name, so the <c>@</c> that
	/// escapes a keyword is dropped, and any other character outside letters, digits, dots, and underscores becomes
	/// <c>_</c>. Dropping the <c>@</c> makes no two names equal, since a keyword can only be written escaped.
	/// </para>
	/// </remarks>
	/// <param name="fullyQualifiedName">The type's fully qualified name.</param>
	/// <returns>The hint name, without the file extension that the caller adds.</returns>
	public static string MakeHintName(string fullyQualifiedName)
	{
		string name = StripGlobalAlias(fullyQualifiedName);
		StringBuilder sb = new(name.Length);
		foreach (char c in name)
		{
			if (c == '@')
				continue;

			sb.Append(char.IsLetterOrDigit(c) || c == '.' || c == '_' ? c : '_');
		}

		return sb.ToString();
	}

	/// <summary>Removes the <c>global::</c> alias from the start of a fully qualified name.</summary>
	/// <param name="fullyQualifiedName">The name to strip.</param>
	/// <returns>The name without its <c>global::</c> prefix.</returns>
	public static string StripGlobalAlias(string fullyQualifiedName)
	{
		return fullyQualifiedName.StartsWith(GLOBAL_ALIAS, StringComparison.Ordinal)
			? fullyQualifiedName.Substring(GLOBAL_ALIAS.Length)
			: fullyQualifiedName;
	}

	/// <summary>Builds the XML doc element that refers to a type in generated documentation.</summary>
	/// <remarks>
	/// A <c>cref</c> can name a generic type's definition, as <c>IGen{T}</c>, but not a constructed type such as
	/// <c>IGen&lt;int&gt;</c>, which gives CS1584 in braces and badly formed XML (CS1570) in angle brackets. A type
	/// with type arguments is written as code instead, with its angle brackets escaped.
	/// </remarks>
	/// <param name="fullyQualifiedName">The type's fully qualified name, as <see cref="SymbolDisplayFormat.FullyQualifiedFormat"/> writes it.</param>
	/// <returns>A <c>see</c> element for a type without type arguments; otherwise, a <c>c</c> element.</returns>
	public static string ToDocReference(string fullyQualifiedName)
	{
		string name = fullyQualifiedName.Replace(GLOBAL_ALIAS, string.Empty);
		return name.IndexOf('<') < 0
			? "<see cref=\"" + name + "\"/>"
			: "<c>" + name.Replace("<", "&lt;").Replace(">", "&gt;") + "</c>";
	}
}
