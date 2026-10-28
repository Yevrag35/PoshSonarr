using MG.Sonarr.Next.Extensions.Strings;
using System.Collections.Concurrent;
using System.Management.Automation;

namespace MG.Sonarr.Next.Extensions.Reflection;

/// <summary>
/// Custom extensions for retrieving names for <see cref="Type"/> instances.
/// </summary>
public static class TypeNameExtensions
{
	private static readonly ConcurrentDictionary<PSTypeKey, string> s_typeNameCache = [];

	/// <summary>
	/// Returns the PowerShell type name for the specified .NET type.
	/// </summary>
	/// <param name="type">The .NET type for which to retrieve the corresponding PowerShell type name. Cannot be null.</param>
	/// <returns>A string containing the PowerShell type name associated with the specified .NET type.</returns>
	public static string GetPSTypeName(this Type type)
	{
		return GetPSTypeName(type, removeBrackets: false);
	}
	/// <summary>
	/// Gets the PowerShell type name for the specified .NET type, optionally removing array or generic type
	/// brackets from the result.
	/// </summary>
	/// <remarks>This method uses PowerShell's type accelerator names when available. If the type is
	/// not accelerated, the full type name is returned, optionally with brackets based on the <paramref
	/// name="removeBrackets"/> parameter.</remarks>
	/// <param name="type">The .NET type for which to retrieve the PowerShell type name. Cannot be null.</param>
	/// <param name="removeBrackets">Specifies whether to remove array or generic type brackets from the returned type name. Set to <see
	/// langword="true"/> to exclude brackets; otherwise, <see langword="false"/> to include them.</param>
	/// <returns>A string containing the PowerShell type name corresponding to the specified .NET type. If no accelerated
	/// name is found, returns the full type name.</returns>
	public static string GetPSTypeName(this Type type, bool removeBrackets)
	{
		if (PSTypeAcceleratorNames.Shared.TryGetName(type, includeBrackets: !removeBrackets, out string? acceleratedName))
		{
			return acceleratedName;
		}

		return s_typeNameCache.GetOrAdd(new PSTypeKey(KeyedType: type, IncludeBrackets: !removeBrackets), GetTypeName);
	}

	/// <summary>
	/// Gets the PowerShell type name for the type that the specified cache key holds.
	/// </summary>
	/// <remarks>
	/// A name without brackets loses only the enclosing pair, so a generic or array name keeps its own brackets:
	/// <c>[string[]]</c> becomes <c>string[]</c>.
	/// </remarks>
	/// <param name="key">The cache key that holds the type and whether its name keeps the enclosing brackets.</param>
	/// <returns>The PowerShell type name, with or without its enclosing brackets as <paramref name="key"/> specifies.</returns>
	private static string GetTypeName(PSTypeKey key)
	{
		string name = LanguagePrimitives.ConvertTypeNameToPSTypeName(key.KeyedType.FullName);
		Debug.Assert(name is not null, "LanguagePrimitives.ConvertTypeNameToPSTypeName returned null for a non-null type name.");

		if (!key.IncludeBrackets && name.EnclosedIn('[', ']'))
		{
			name = name[1..^1];
		}

		return name ?? string.Empty;
	}

	/// <summary>
	/// Represents a key of the type name cache: a type and whether its name keeps the enclosing brackets.
	/// </summary>
	/// <param name="KeyedType">The type to name.</param>
	/// <param name="IncludeBrackets">A value that indicates whether the name keeps its enclosing brackets.</param>
	private readonly record struct PSTypeKey(Type KeyedType, bool IncludeBrackets);
}
