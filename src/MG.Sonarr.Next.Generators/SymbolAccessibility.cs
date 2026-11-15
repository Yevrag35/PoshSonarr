using Microsoft.CodeAnalysis;

namespace MG.Sonarr.Next.Generators;

/// <summary>
/// Provides the check of whether a generated class at namespace scope can name a type, and how visible that lets the
/// class be.
/// </summary>
internal static class SymbolAccessibility
{
	/// <summary>
	/// Narrows a generated class's accessibility to what a type allows, or finds the type that the class can't name.
	/// </summary>
	/// <remarks>
	/// A generated class at namespace scope names a nested type through every type that contains it, and a constructed
	/// type through each of its type arguments. The class is public only when all of them are public, and internal when
	/// one of them is internal or <see langword="protected"/> <see langword="internal"/>, so a public generated member
	/// never names a type that callers outside the assembly couldn't name.
	/// </remarks>
	/// <param name="type">The type that the generated class names.</param>
	/// <param name="accessibility">
	/// The accessibility so far, <see cref="Accessibility.Public"/> or <see cref="Accessibility.Internal"/>, which this
	/// method lowers to internal when <paramref name="type"/> calls for it.
	/// </param>
	/// <param name="hidden">
	/// When this method returns <see langword="false"/>, contains the phrase that names the type the class can't name,
	/// such as <c>'Hidden' is private</c>; otherwise, an empty string.
	/// </param>
	/// <returns><see langword="true"/> if the generated class can name <paramref name="type"/>; otherwise, <see langword="false"/>.</returns>
	public static bool TryNarrow(ITypeSymbol type, ref Accessibility accessibility, out string hidden)
	{
		switch (type)
		{
			case IArrayTypeSymbol array:
				return TryNarrow(array.ElementType, ref accessibility, out hidden);

			case INamedTypeSymbol named:
				for (INamedTypeSymbol? current = named; current is not null; current = current.ContainingType)
				{
					if (current.IsFileLocal)
					{
						hidden = $"'{current.Name}' is file-local";
						return false;
					}

					switch (current.DeclaredAccessibility)
					{
						case Accessibility.Internal:
						case Accessibility.ProtectedOrInternal:
							accessibility = Accessibility.Internal;
							break;

						case Accessibility.Private:
							hidden = $"'{current.Name}' is private";
							return false;

						case Accessibility.Protected:
							hidden = $"'{current.Name}' is protected";
							return false;

						case Accessibility.ProtectedAndInternal:
							hidden = $"'{current.Name}' is private protected";
							return false;
					}
				}

				foreach (ITypeSymbol argument in named.TypeArguments)
				{
					if (!TryNarrow(argument, ref accessibility, out hidden))
						return false;
				}

				hidden = string.Empty;
				return true;

			default:
				hidden = string.Empty;
				return true;
		}
	}
}
