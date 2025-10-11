using System.Collections;

namespace MG.Sonarr.Next.Collections
{
    [DebuggerStepThrough]
    [StructLayout(LayoutKind.Auto)]
    internal readonly struct OneStringDictionary : IReadOnlyDictionary<string, string>
    {
        const int COUNT = 1;

        readonly string? _key;
        readonly string? _value;

        [MemberNotNullWhen(false, nameof(_key), nameof(_value))]
        public bool IsEmpty => string.IsNullOrEmpty(_key) || string.IsNullOrEmpty(_value);
        public string Key => _key ?? string.Empty;
        public string Value => _value ?? string.Empty;

        int IReadOnlyCollection<KeyValuePair<string, string>>.Count => COUNT;
        IEnumerable<string> IReadOnlyDictionary<string, string>.Keys
        {
            [DebuggerStepThrough]
            get
            {
                return [this.Key];
            }
        }
        IEnumerable<string> IReadOnlyDictionary<string, string>.Values
        {
            [DebuggerStepThrough]
            get
            {
                return [this.Value];
            }
        }
        string IReadOnlyDictionary<string, string>.this[string key] => this.GetValueIfKeyIsSame(key, failOnNotEqual: true);

        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="KeyNotFoundException"></exception>
        private string GetValueIfKeyIsSame(string key, bool failOnNotEqual)
        {
            ArgumentNullException.ThrowIfNull(key);
            if (!this.IsEmpty && key.Equals(_key, StringComparison.OrdinalIgnoreCase))
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
        }
        internal OneStringDictionary(KeyValuePair<string, string> pair)
            : this(pair.Key, pair.Value)
        {
        }

        bool IReadOnlyDictionary<string, string>.ContainsKey(string key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return !this.IsEmpty
                && key.AsSpan().Equals(_key, StringComparison.OrdinalIgnoreCase);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            if (!this.IsEmpty)
            {
                KeyValuePair<string, string> kvp = new(_key, _value);
                return Enumerable.Repeat(kvp, 1).GetEnumerator();
            }

            return Enumerable.Empty<KeyValuePair<string, string>>().GetEnumerator();
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
