using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Services.Extensions
{
    public static class DictionaryExtensions
    {
        public static ReadOnlyDictionary<TKey, TValue> ToReadOnly<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>>? items, IEqualityComparer<TKey>? comparer = null) where TKey : notnull
        {
            if (items is null)
            {
                return new(new Dictionary<TKey, TValue>());
            }

            int capacity = items.TryGetNonEnumeratedCount(out int count) ? count : 0;
            Dictionary<TKey, TValue> dict = new(capacity, comparer);

            foreach (var kvp in items)
            {
                _ = dict.TryAdd(kvp.Key, kvp.Value);
            }

            return new(dict);
        }

        public static bool TryGetValueAs<TKey, TOutput>(this IReadOnlyDictionary<TKey, object?> dictionary, TKey key, [NotNullWhen(true)] out TOutput? result) where TKey : notnull
        {
            result = default;
            if (dictionary.TryGetValue(key, out object? value) && value is TOutput tOut)
            {
                result = tOut;
                return result is not null;
            }

            return false;
        }
    }
}
