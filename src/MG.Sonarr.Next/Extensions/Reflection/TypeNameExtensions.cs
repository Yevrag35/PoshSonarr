using MG.Sonarr.Next.Components;
using MG.Sonarr.Next.Extensions.Strings;
using System.Collections.Concurrent;
using System.Management.Automation;

namespace MG.Sonarr.Next.Extensions.Reflection
{
    /// <summary>
    /// Custom extensions for retrieving names for <see cref="Type"/> instances.
    /// </summary>
    public static class TypeNameExtensions
    {
        private static readonly ConcurrentDictionary<PSTypeKey, string> _typeNameCache = [];

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

            return _typeNameCache.GetOrAdd(new PSTypeKey(type, includeBrackets: !removeBrackets), GetTypeName);
        }

        private static string GetTypeName(PSTypeKey key)
        {
            string name = LanguagePrimitives.ConvertTypeNameToPSTypeName(key.KeyedType!.FullName);
            if (!key.IncludeBrackets && name.EnclosedIn('[', ']'))
            {
                name = name.AsSpan(1, name.Length - 2)
                           .TrimStart('[')
                           .TrimEnd(']')
                           .Trim()
                           .ToString();
            }

            return name;
        }
    }
}
