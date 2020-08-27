using MG.Sonarr.Functionality;
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
    public class AllowedQualityCollection : SortedListBase<int, AllowedQuality>, IReadOnlyList<AllowedQuality>
    {
        [JsonIgnore]
        private Dictionary<string, int> _map;

        #region INDEXERS
        public AllowedQuality this[int index]
        {
            get
            {
                int posIndex = this.GetPositiveIndex(index);
                return posIndex > -1 ? base.InnerList.Values[posIndex] : null;
            }
        }
        public AllowedQuality this[string name] => _map.ContainsKey(name) ? base.InnerList[_map[name]] : null;

        #endregion

        #region CONSTRUCTORS
        [JsonConstructor]
        internal AllowedQualityCollection(IEnumerable<AllowedQuality> qualityItems)
            : base(qualityItems, q => q.Id)
        {
            _map = base.InnerList.ToDictionary(x => x.Value.Name, x => x.Key, SonarrFactory.NewIgnoreCase());
        }

        #endregion

        #region METHODS
        internal void Allow(IEnumerable<IQuality> allowables)
        {
            foreach (AllowedQuality aq in base.InnerList.Values.Intersect(allowables))
            {
                aq.Allowed = true;
            }
        }
        internal void Allow(params string[] names)
        {
            if (names == null || names.Length <= 0)
                return;

            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                if (_map.ContainsKey(name))
                    base.InnerList[_map[name]].Allowed = true;
            }
        }
        internal void Allow(params int[] ids)
        {
            if (ids == null || ids.Length <= 0)
                return;

            for (int i = 0; i < ids.Length; i++)
            {
                int id = ids[i];
                if (base.InnerList.ContainsKey(id))
                {
                    base.InnerList[id].Allowed = true;
                }
            }
        }
        public bool Contains(int id)
        {
            return base.InnerList.ContainsKey(id);
        }
        public bool Contains(string qualityName)
        {
            return _map.ContainsKey(qualityName);
        }
        public bool Contains(IQuality quality)
        {
            return this.Contains(quality.Id);
        }
        internal void Disallow(IEnumerable<IQuality> disallowables)
        {
            foreach (AllowedQuality aq in base.InnerList.Values.Intersect(disallowables))
            {
                aq.Allowed = false;
            }
        }
        internal void Disallow(string name)
        {
            if (_map.ContainsKey(name))
                base.InnerList[_map[name]].Allowed = false;
        }
        //internal void Disallow(params int[] ids)
        //{
        //    if (ids == null || ids.Length <= 0)
        //        return;

        //    for (int i = 0; i < ids.Length; i++)
        //    {
        //        int id = ids[i];
        //        if (_map.ContainsKey(id))
        //        {
        //            base.InnerList[_map[id]].Allowed = false;
        //        }
        //    }
        //}
        //public AllowedQuality GetById(int id)
        //{
        //    AllowedQuality aq = null;
        //    if (_map.ContainsKey(id))
        //    {
        //        aq = base.InnerList[_map[id]];
        //    }
        //    return aq;
        //}
        public bool IsAllowed(int qualityId)
        {
            bool result = false;
            if (base.InnerList.TryGetValue(qualityId, out AllowedQuality found))
                result = found.Allowed;

            return result;
        }

        #endregion

        public static explicit operator AllowedQualityCollection(List<Quality> qualities) => 
            new AllowedQualityCollection(qualities.Select(x => AllowedQuality.FromQuality(x, false)));
    }
}
