using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality.Strings
{
    internal class ReadOnlyKeySet : IReadOnlyCollection<string>, IReadOnlyList<string>
    {
        private StringComparison _comparison = StringComparison.CurrentCultureIgnoreCase;
        private HashSet<string> _keys;

        public string this[string key] => _keys.FirstOrDefault(x => x.Equals(key, _comparison));
        public string this[int index] => _keys.ElementAt(index);
        public int Count => _keys.Count;
        public bool IsCaseSensitive
        {
            get => _comparison != StringComparison.CurrentCultureIgnoreCase;
            set => _comparison = value ?
                StringComparison.CurrentCulture :
                StringComparison.CurrentCultureIgnoreCase;
        }

        public ReadOnlyKeySet(IEnumerable<string> keys)
        {
            _keys = new HashSet<string>(keys, SonarrFactory.NewIgnoreCase());
        }

        public bool ContainsKey(string key) => _keys.Any(x => x.Equals(key, _comparison));
        public IEnumerator<string> GetEnumerator() => _keys.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _keys.GetEnumerator();
    }
}
