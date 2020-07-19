using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality
{
    public sealed class IndexerSchemaDictionary : IReadOnlyDictionary<string, IndexerSchema>
    {
        private ReadOnlyKeySet _keys;
        private Dictionary<string, IndexerSchema> _dict;

        public IndexerSchema this[string key]
        {
            get
            {
                string realKey = _keys[key];
                return _dict[realKey] ?? null;
            }
        }

        public int Count => _dict.Count;
        public IEnumerable<string> Keys => _keys;
        IEnumerable<string> IReadOnlyDictionary<string, IndexerSchema>.Keys => _dict.Keys;
        public IEnumerable<IndexerSchema> Values => _dict.Values;

        public IndexerSchemaDictionary(IEnumerable<IndexerSchema> schemas)
        {
            _dict = new Dictionary<string, IndexerSchema>(12);
            foreach (IndexerSchema schema in schemas)
            {
                _dict.Add(schema.ConfigContract, schema);
            }
            _keys = new ReadOnlyKeySet(_dict.Keys);
        }

        public bool ContainsKey(string key) => _dict.ContainsKey(key);
        public IEnumerator<KeyValuePair<string, IndexerSchema>> GetEnumerator() => _dict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();
        public bool TryGetValue(string key, out IndexerSchema value)
            => _dict.TryGetValue(key, out value);
    }
}
