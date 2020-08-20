using MG.Sonarr.Functionality;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    public class EpisodeComparison : IReadOnlyDictionary<string, object>
    {
        private const string EP_PROP = "EpisodeNumber{0}";
        private const string EP_PROP_NAME = EP_PROP + "Name";
        private Dictionary<string, object> _dict;

        internal EpisodeComparison(string fileName, List<IEpisode> episodes, bool showName)
            : base()
        {
            _dict = new Dictionary<string, object>(2)
            {
                { "Name", fileName }
            };
            for (int i = 1; i <= episodes.Count; i++)
            {
                IEpisode ep = episodes[i - 1];
                string name = string.Format(EP_PROP, i);
                _dict.Add(name, ep.EpisodeNumber);
                if (showName)
                {
                    string other = string.Format(EP_PROP_NAME, i);
                    _dict.Add(other, ep.Name);
                }
            }
        }

        public object this[string key] => _dict[key];

        public IEnumerable<string> Keys => ((IReadOnlyDictionary<string, object>)_dict).Keys;

        public IEnumerable<object> Values => ((IReadOnlyDictionary<string, object>)_dict).Values;

        public int Count => _dict.Count;

        public bool ContainsKey(string key) => _dict.ContainsKey(key);
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, object>>)_dict).GetEnumerator();
        public bool TryGetValue(string key, out object value) => _dict.TryGetValue(key, out value);
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_dict).GetEnumerator();
    }
}
