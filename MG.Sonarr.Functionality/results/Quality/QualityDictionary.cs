using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Functionality.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [Serializable]
    public class QualityDictionary : SortedStringList<Quality>, IReadOnlyDictionary<string, Quality>, IReadOnlyList<Quality>
    {
        private Dictionary<int, string> _map;

        #region INDEXERS
        public Quality this[int index]
        {
            get
            {
                int posIndex = this.GetPositiveIndex<Quality>(index);
                return posIndex > -1 ? base.InnerList.Values[posIndex] : null;
            }
        }

        #endregion

        #region PROPERTIES
        public IEnumerable<int> Ids => _map.Keys;
        IEnumerable<string> IReadOnlyDictionary<string, Quality>.Keys => base.InnerList.Keys;
        IEnumerable<Quality> IReadOnlyDictionary<string, Quality>.Values => base.InnerList.Values;

        #endregion

        #region CONSTRUCTORS
        [JsonConstructor]
        internal QualityDictionary(IEnumerable<Quality> qualityItems)
            : base(qualityItems, x => x.Name)
        {
            _map = base.InnerList.ToDictionary(x => x.Value.Id, x => x.Key);
        }

        #endregion

        #region METHODS
        public bool ContainsId(int id)
        {
            return _map.ContainsKey(id);
        }
        public bool ContainsKey(string name)
        {
            return base.InnerList.ContainsKey(name);
        }
        public static QualityDictionary FromDefinitions(IEnumerable<QualityDefinition> definitions)
        {
            return new QualityDictionary(definitions.Select(x => x.Quality));
        }
        public Quality GetById(int id)
        {
            return this.ContainsId(id) ? base.InnerList[_map[id]] : null;
        }
        public bool TryGetValue(string name, out Quality value)
        {
            value = null;
            if (base.InnerList.TryGetValue(name, out Quality found))
                value = found;

            return value != null;
        }

        #endregion

        #region ENUMERATORS
        IEnumerator<KeyValuePair<string, Quality>> IEnumerable<KeyValuePair<string, Quality>>.GetEnumerator()
        {
            return base.InnerList.GetEnumerator();
        }

        #endregion
    }
}