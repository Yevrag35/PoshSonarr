using System.Reflection;

namespace MG.Sonarr.Next.Extensions
{
    /// <summary>
    /// Custom extensions for the <see cref="Type"/> class.
    /// </summary>
    public static class TypeExtensions
    {
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
        [return: NotNullIfNotNull(nameof(type))]
        public static string? GetTypeName(this Type? type)
        {
            return type?.FullName ?? type?.Name;
        }
    }
}
