using System.Collections;

namespace MG.Sonarr.Next.Services.Json.Collections
{
    public sealed class JsonNameDictionary : IReadOnlyCollection<KeyValuePair<string, string>>
    {
        readonly Dictionary<string, string> _forDe;
        readonly Dictionary<string, string> _forSer;

        public int Count => _forDe.Count + _forSer.Count;

        public JsonNameDictionary(int capacity)
        {
            _forDe = new(capacity, StringComparer.InvariantCultureIgnoreCase);
            _forSer = new(capacity, StringComparer.InvariantCultureIgnoreCase);
        }
        public JsonNameDictionary(IEnumerable<KeyValuePair<string, string>> pairs)
            : this(pairs.TryGetNonEnumeratedCount(out int count) ? count : 5)
        {
            foreach (KeyValuePair<string, string> kvp in pairs)
            {
                _forDe.Add(kvp.Key, kvp.Value);
                _forSer.Add(kvp.Value, kvp.Key);
            }
        }

        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public void Add(string serializedValue, string replaceWith)
        {
            ArgumentException.ThrowIfNullOrEmpty(serializedValue);
            ArgumentException.ThrowIfNullOrEmpty(replaceWith);

            _forDe.Add(serializedValue, replaceWith);
            _forSer.Add(replaceWith, serializedValue);
        }

        public IReadOnlyDictionary<string, string> ForDeserializing() => _forDe;
        public IReadOnlyDictionary<string, string> ForSerializing() => _forSer;

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (var kvp in _forDe)
            {
                yield return kvp;
            }

            foreach (var kvp in _forSer)
            {
                yield return kvp;
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
