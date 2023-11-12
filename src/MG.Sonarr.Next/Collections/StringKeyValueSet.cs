using System.Collections;

namespace MG.Sonarr.Next.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    public sealed class StringKeyValueSet<T> : SortedSet<KeyValuePair<string, T>>
    {
        public T? this[string key]
        {
            get => this.TryGetValue(key, out T? value)
                ? value
                : default;
            set
            {
                if (value is null)
                {
                    return;
                }

                var kvp = ToKeyPair(key, value);
                this.Remove(kvp);
                this.Add(kvp);
            }
        }

        public StringKeyValueSet()
            : base(KeyValueSetComparers.GetOrCreate<T>())
        {
        }
        public StringKeyValueSet(IEnumerable<KeyValuePair<string, T>> values)
            : base(values, KeyValueSetComparers.GetOrCreate<T>())
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <returns></returns>
        public bool Add(string key, T value)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            ArgumentNullException.ThrowIfNull(value);

            return this.Add(ToKeyPair(key, value));
        }
        public bool ContainsKey(string key)
        {
            return this.Contains(ToKeyPair(key));
        }
        public bool Remove(string key)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            return this.Remove(ToKeyPair(key));
        }
        public bool TryGetValue(string key, [NotNullWhen(true)] out T? value)
        {
            value = default;
            if (this.TryGetValue(ToKeyPair(key), out KeyValuePair<string, T> kvp))
            {
                value = kvp.Value;
                return value is not null;
            }

            return false;
        }

        private static KeyValuePair<string, T> ToKeyPair(string key)
        {
            return new(key, default!);
        }
        private static KeyValuePair<string, T> ToKeyPair(string key, T value)
        {
            return new(key, value);
        }
        
    }

    file static class KeyValueSetComparers
    {
        internal static IComparer<KeyValuePair<string, T>> GetOrCreate<T>()
        {
            KeyValueComparer<T> comparer;
            if (!_comparers.IsValueCreated || !_comparers.Value.TryGetValue(typeof(T), out KeyValueComparer? comp))
            {
                comparer = new();
                _comparers.Value.TryAdd(comparer.ComparesType, comparer);
            }
            else
            {
                comparer = (KeyValueComparer<T>)comp;
            }

            return comparer;
        }

        static readonly Lazy<Dictionary<Type, KeyValueComparer>> _comparers = new(() => new(1));

        private sealed class KeyValueComparer<T> : KeyValueComparer, IComparer<KeyValuePair<string, T>>
        {
            internal override Type ComparesType { get; }

            internal KeyValueComparer()
            {
                this.ComparesType = typeof(T);
            }

            public int Compare(KeyValuePair<string, T> x, KeyValuePair<string, T> y)
            {
                return StringComparer.InvariantCultureIgnoreCase.Compare(x.Key, y.Key);
            }
        }
        private abstract class KeyValueComparer
        {
            internal abstract Type ComparesType { get; }
        }
    }
}

