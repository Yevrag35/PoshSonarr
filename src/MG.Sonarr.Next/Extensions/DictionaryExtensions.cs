using System.Collections.ObjectModel;

namespace MG.Sonarr.Next.Extensions
{
    /// <summary>
    /// Custom extension methods for specific types of <see cref="Dictionary{TKey, TValue}"/>.
    /// </summary>
    public static class DictionaryExtensions
    {
        public static string GetValue<T>(this IReadOnlyDictionary<T, string>? dictionary, T key) where T : notnull
        {
            return dictionary is not null && dictionary.TryGetValue(key, out string? value)
                ? value
                : string.Empty;
        }

        [return: NotNullIfNotNull(nameof(dictionary))]
        public static ReadOnlyDictionary<TKey, TValue>? ToReadOnly<TKey, TValue>(this Dictionary<TKey, TValue>? dictionary) where TKey : notnull
        {
            if (dictionary is null)
            {
                return null;
            }

            return new ReadOnlyDictionary<TKey, TValue>(dictionary);
        }

        /// <summary>
        /// Extends <see cref="Dictionary{TKey, TValue}.TryGetValue(TKey, out TValue)"/> and 
        /// additionally attempts to cast the found object instance to 
        /// <typeparamref name="TOutput"/>.
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the read-only dictionary</typeparam>
        /// <typeparam name="TOutput">The type of values in the read-only dictionary</typeparam>
        /// <param name="dictionary">The dictionary to find the value from.</param>
        /// <param name="key">The key to locate</param>
        /// <param name="value">
        ///     When this method returns, the value of type <typeparamref name="TOutput"/>
        ///     associated with the specified key if <paramref name="key"/> is found; otherwise, 
        ///     the default value for <typeparamref name="TOutput"/>.
        ///     <para>This parameter is passed uninitialized.</para>
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the base 
        ///     <see cref="Dictionary{TKey, TValue}.TryGetValue(TKey, out TValue)"/> returns 
        ///     <see langword="true"/> and the object instance is not <see langword="null"/>
        ///     of the type <typeparamref name="TOutput"/>.
        /// </returns>
        public static bool TryGetValueAs<TKey, TOutput>(this IReadOnlyDictionary<TKey, object?> dictionary, TKey key, [NotNullWhen(true)] out TOutput? value) where TKey : notnull
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            ArgumentNullException.ThrowIfNull(key);

            value = default;
            if (dictionary.TryGetValue(key, out object? dictVal) && dictVal is TOutput tOut)
            {
                value = tOut;
                return value is not null;
            }

            return false;
        }
    }
}
