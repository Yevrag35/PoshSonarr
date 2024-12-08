using MG.Sonarr.Next.Components;
using MG.Sonarr.Next.Extensions.Strings;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Next.Extensions.Reflection
{
    /// <summary>
    /// Custom extensions for the <see cref="Type"/> class.
    /// </summary>
    public static class TypeNameExtensions
    {
        private static readonly ConcurrentDictionary<PSTypeKey, string> _typeNameCache = [];

        /// <summary>
        /// Returns the <see cref="Type"/> class's name for display or logging purposes.
        /// </summary>
        /// <remarks>
        ///     Only if the input parameter is <see langword="null"/>, will the returned
        ///     <see cref="string"/> be also <see langword="null"/>.
        /// </remarks>
        /// <param name="type">The type whose name is returned.</param>
        /// <returns>
        ///     By default, <see cref="Type.FullName"/>, or, if <see langword="null"/>, 
        ///     <see cref="MemberInfo.Name"/>.
        /// </returns>
        public static string GetName(this Type type)
        {
            return type.FullName ?? type.Name;
        }
        /// <summary>
        /// Returns the fully qualified name or the member name of the current <see cref="Type"/> -or- 
        /// <paramref name="otherValue"/> if the Type instance is <see langword="null"/>.
        /// </summary>
        /// <param name="type">The type whose name will be returned.</param>
        /// <returns>
        /// The <see cref="Type.FullName"/> of the current <see cref="Type"/> if it is not <see langword="null"/>;
        /// otherwise, the <see cref="MemberInfo.Name"/> instead.  If the type being extended is <see langword="null"/>,
        /// then <paramref name="otherValue"/> is returned.
        /// </returns>
        [DebuggerStepThrough]
        [return: NotNullIfNotNull(nameof(otherValue))]
        public static string? GetNameOr(this Type? type, string? otherValue)
        {
            return type is not null
                ? type.GetName()
                : otherValue;
        }
        /// <summary>
        /// Returns the fully qualified name or the member name of the current <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The type whose name will be returned.</param>
        /// <returns>
        /// The <see cref="Type.FullName"/> of the current <see cref="Type"/> if it is not <see langword="null"/>;
        /// otherwise, the <see cref="MemberInfo.Name"/> instead.  If the type being extended is <see langword="null"/>,
        /// then <see langword="null"/> is returned.
        /// </returns>
        [DebuggerStepThrough]
        [return: NotNullIfNotNull(nameof(type))]
        public static string? GetNameOrNull(this Type? type)
        {
            return type.GetNameOr(otherValue: null);
        }

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
