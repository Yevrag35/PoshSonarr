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
    public class AllowedQualityCollection : SortedStringList<AllowedQuality>, IReadOnlyList<AllowedQuality>
    {
        [JsonIgnore]
        private Dictionary<int, string> _map;

        #region INDEXERS
        public AllowedQuality this[int index]
        {
            get
            {
                int posIndex = this.GetPositiveIndex(index);
                return posIndex > -1 ? base.InnerList.Values[posIndex] : null;
            }
        }

        #endregion

        #region CONSTRUCTORS
        [JsonConstructor]
        internal AllowedQualityCollection(IEnumerable<AllowedQuality> qualityItems)
            : base(qualityItems, q => q.Quality.Name)
        {
            _map = base.InnerList.ToDictionary(x => x.Value.Quality.Id, x => x.Key);
        }

        #endregion

        #region METHODS
        internal void Allow(IEnumerable<Quality> allowables)
        {
            foreach (Quality q in allowables)
            {
                if (_map.ContainsKey(q.Id))
                {
                    base.InnerList[_map[q.Id]].Allowed = true;
                }
            }
        }
        internal void Allow(params string[] names)
        {
            if (names == null || names.Length <= 0)
                return;

            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                if (base.InnerList.ContainsKey(name))
                    base.InnerList[name].Allowed = true;
            }
        }
        internal void Allow(params int[] ids)
        {
            if (ids == null || ids.Length <= 0)
                return;

            for (int i = 0; i < ids.Length; i++)
            {
                int id = ids[i];
                if (_map.ContainsKey(id))
                {
                    base.InnerList[_map[id]].Allowed = true;
                }
            }
        }
        public bool ContainsId(int id)
        {
            return _map.ContainsKey(id);
        }
        public bool Contains(string qualityName)
        {
            return base.InnerList.ContainsKey(qualityName);
        }
        internal void Disallow(IEnumerable<Quality> disallowables)
        {
            foreach (Quality q in disallowables)
            {
                if (_map.ContainsKey(q.Id))
                {
                    base.InnerList[_map[q.Id]].Allowed = false;
                }
            }
        }
        internal void Disallow(params string[] names)
        {
            if (names == null || names.Length <= 0)
                return;

            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                if (base.InnerList.ContainsKey(name))
                    base.InnerList[name].Allowed = false;
            }
        }
        internal void Disallow(params int[] ids)
        {
            if (ids == null || ids.Length <= 0)
                return;

            for (int i = 0; i < ids.Length; i++)
            {
                int id = ids[i];
                if (_map.ContainsKey(id))
                {
                    base.InnerList[_map[id]].Allowed = false;
                }
            }
        }
        public AllowedQuality GetById(int id)
        {
            AllowedQuality aq = null;
            if (_map.ContainsKey(id))
            {
                aq = base.InnerList[_map[id]];
            }
            return aq;
        }
        public bool IsAllowed(int qualityId)
        {
            bool result = false;
            if (_map.ContainsKey(qualityId))
            {
                result = base.InnerList[_map[qualityId]].Allowed;
            }
            return result;
        }

        #endregion

        public static explicit operator AllowedQualityCollection(List<Quality> qualities) => 
            new AllowedQualityCollection(qualities.Select(x => AllowedQuality.FromQuality(x, false)));
    }
}
