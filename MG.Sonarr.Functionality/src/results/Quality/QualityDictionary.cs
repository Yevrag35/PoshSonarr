using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace MG.Sonarr.Functionality
{
    public class QualityDictionary : IReadOnlyDictionary<string, Quality>, IEnumerable<Quality>
    {
        private IEqualityComparer<string> _comparer;
        private Dictionary<string, Quality> _dict;

        public Quality this[string name] => _dict[name.ToLower()];
        public Quality this[int id]
        {
            get
            {
                return _dict.SingleOrDefault(x => x.Value.Id.Equals(id)).Value;
            }
        }

        public int Count => _dict.Count;
        public IEnumerable<string> Keys => _dict.Keys;
        IEnumerable<Quality> IReadOnlyDictionary<string, Quality>.Values => _dict.Values;

        public QualityDictionary(IEnumerable<Quality> qualities)
        {
            _comparer = ClassFactory.NewIgnoreCase();
            _dict = new Dictionary<string, Quality>(14);
            foreach (Quality qal in qualities.OrderBy(x => x.Id))
            {
                _dict.Add(qal.Name.ToLower(), qal);
            }
        }

        public bool ContainsKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            return _dict.ContainsKey(key.ToLower());
        }
        public bool TryGetValue(string key, out Quality value)
        {
            value = null;
            bool result = false;
            if (!string.IsNullOrEmpty(key) && _dict.TryGetValue(key.ToLower(), out Quality qal))
            {
                value = qal;
                result = true;
            }

            return result;
        }

        IEnumerator<Quality> IEnumerable<Quality>.GetEnumerator() => _dict.Values.GetEnumerator();
        IEnumerator<KeyValuePair<string, Quality>> IEnumerable<KeyValuePair<string, Quality>>.GetEnumerator() => _dict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();
    }
}
