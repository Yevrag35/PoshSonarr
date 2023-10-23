using System.Collections;

namespace MG.Sonarr.Next.Collections
{
    internal readonly struct OneStringDictionary : IReadOnlyDictionary<string, string>
    {
        const int COUNT = 1;

        readonly bool _isNotEmpty;
        readonly string? _key;
        readonly string? _value;

        [MemberNotNullWhen(false, nameof(_key), nameof(_value))]
        public bool IsEmpty => !_isNotEmpty;
        public string Key => _key ?? string.Empty;
        public string Value => _value ?? string.Empty;

        int IReadOnlyCollection<KeyValuePair<string, string>>.Count => COUNT;
        IEnumerable<string> IReadOnlyDictionary<string, string>.Keys
        {
            get
            {
                yield return _key ?? string.Empty;
            }
        }
        IEnumerable<string> IReadOnlyDictionary<string, string>.Values
        {
            get
            {
                yield return _value ?? string.Empty;
            }
        }
        string IReadOnlyDictionary<string, string>.this[string key] => this.GetValueIfKeyIsSame(key, failOnNotEqual: true);

        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="KeyNotFoundException"></exception>
        private string GetValueIfKeyIsSame(string key, bool failOnNotEqual)
        {
            ArgumentNullException.ThrowIfNull(key);
            if (!this.IsEmpty && key.Equals(_key, StringComparison.InvariantCultureIgnoreCase))
            {
                return _value;
            }
            else if (failOnNotEqual)
            {
                throw new KeyNotFoundException($"{key} does not match the single key value of {_key}.");
            }

            return string.Empty;
        }

        internal OneStringDictionary(string key, string value)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            ArgumentException.ThrowIfNullOrEmpty(value);

            _key = key;
            _value = value;
            _isNotEmpty = true;
        }
        internal OneStringDictionary(KeyValuePair<string, string> pair)
            : this(pair.Key, pair.Value)
        {
        }

        bool IReadOnlyDictionary<string, string>.ContainsKey(string key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return !this.IsEmpty && StringComparer.InvariantCultureIgnoreCase.Equals(key, this.Key);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            if (!this.IsEmpty)
            {
                yield return new(this.Key, this.Value);
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        bool IReadOnlyDictionary<string, string>.TryGetValue(string key, out string value)
        {
            ArgumentNullException.ThrowIfNull(key);

            value = this.GetValueIfKeyIsSame(key, failOnNotEqual: false);
            return !string.IsNullOrEmpty(value);
        }
        internal static OneStringDictionary FromEnumerable(IEnumerable<KeyValuePair<string, string>> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            foreach (KeyValuePair<string, string> kvp in collection)
            {
                return new OneStringDictionary(kvp);
            }

            return default;
        }
    }
}
