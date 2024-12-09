using MG.Sonarr.Next.Components;
using MG.Sonarr.Next.Extensions.Strings;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Next.Extensions.Reflection
{
    /// <summary>
    /// Custom extensions for retrieving names for <see cref="Type"/> instances.
    /// </summary>
    public static class TypeNameExtensions
    {
        private static readonly ConcurrentDictionary<PSTypeKey, string> _typeNameCache = [];

        public static string GetPSTypeName(this Type type)
        {
            return GetPSTypeName(type, removeBrackets: false);
        }
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
