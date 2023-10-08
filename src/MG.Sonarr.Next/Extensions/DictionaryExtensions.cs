using System.Collections.ObjectModel;

namespace MG.Sonarr.Next.Extensions
{
    /// <summary>
    /// Custom extension methods for specific types of <see cref="Dictionary{TKey, TValue}"/>.
    /// </summary>
    public static class DictionaryExtensions
    {
        ///// <summary>
        ///// Enumerates through a collection of <see cref="KeyValuePair{TKey, TValue}"/> instances
        ///// and copies them to a new <see cref="ReadOnlyDictionary{TKey, TValue}"/>.
        ///// </summary>
        ///// <typeparam name="TKey"></typeparam>
        ///// <typeparam name="TValue"></typeparam>
        ///// <param name="items"></param>
        ///// <param name="comparer"></param>
        ///// <returns></returns>
        //public static ReadOnlyDictionary<TKey, TValue> ToReadOnly<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>>? items, IEqualityComparer<TKey>? comparer = null) where TKey : notnull
        //{
        //    if (items is null)
        //    {
        //        return new(new Dictionary<TKey, TValue>());
        //    }

        //    int capacity = items.TryGetNonEnumeratedCount(out int count) ? count : 0;
        //    Dictionary<TKey, TValue> dict = new(capacity, comparer);

        //    foreach (var kvp in items)
        //    {
        //        _ = dict.TryAdd(kvp.Key, kvp.Value);
        //    }

        //    return new(dict);
        //}

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
        public static bool TryGetValueAs<TKey, TOutput>(this IReadOnlyDictionary<TKey, object?>? dictionary, TKey key, [NotNullWhen(true)] out TOutput? value) where TKey : notnull
        {
            value = default;
            if (dictionary is not null
                &&
                dictionary.TryGetValue(key, out object? dictVal)
                &&
                dictVal is TOutput tOut)
            {
                value = tOut;
                return value is not null;
            }

            return false;
        }
    }
}
