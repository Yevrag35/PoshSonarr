using MG.Sonarr.Results.Collections;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace MG.Sonarr.Results
{
    [Serializable]
    public class AllowedQualityCollection : ResultListBase<AllowedQuality>
    {
        internal SortedSet<string> Allowed { get; }
        internal SortedSet<string> Disallowed { get; }

        #region INDEXERS

        public AllowedQuality this[string qualityName] => this.Find(x => x.Quality.Name.Equals(qualityName, StringComparison.CurrentCultureIgnoreCase));

        #endregion

        #region CONSTRUCTORS
        [JsonConstructor]
        internal AllowedQualityCollection(IEnumerable<AllowedQuality> qualityItems) : base(qualityItems)
        {
            this.Allowed = new SortedSet<string>(qualityItems.Where(x => x.Allowed).Select(x => x.Quality.Name), StringComparer.CurrentCultureIgnoreCase);
            this.Disallowed = new SortedSet<string>(qualityItems.Where(x => !x.Allowed).Select(x => x.Quality.Name), StringComparer.CurrentCultureIgnoreCase);
        }

        #endregion

        #region METHODS
        internal void Allow(IEnumerable<Quality> allowables)
        {
            base.InnerList.ForEach((x) =>
            {
                if (allowables.Contains(x.Quality))
                {
                    x.Allowed = true;
                }
            });
        }
        public bool ContainsQualityById(int qualityId) => base.InnerList.Exists(x => x.Quality.Id.Equals(qualityId));
        public bool ContainsQualityByName(string qualityName)
        {
            StringComparison comparison = StringComparison.CurrentCultureIgnoreCase;
            return base.InnerList.Exists(x => x.Quality.Name.Equals(qualityName, comparison));
        }
        internal void Disallow(IEnumerable<Quality> disallowables)
        {
            base.InnerList.ForEach((x) =>
            {
                if (disallowables.Contains(x.Quality))
                {
                    x.Allowed = false;
                }
            });
        }
        internal int FindIndex(Predicate<AllowedQuality> match) => base.InnerList.FindIndex(match);
        internal void ForEach(Action<AllowedQuality> action) => base.InnerList.ForEach(action);
        public AllowedQuality GetAllowedQualityByName(string qualityName)
        {
            StringComparison comparison = StringComparison.CurrentCultureIgnoreCase;
            return base.InnerList.Find(x => x.Quality.Name.Equals(qualityName, comparison));
        }
        public AllowedQuality GetAllowedQualityById(int qualityId) => base.InnerList.Find(x => x.Quality.Id.Equals(qualityId));
        public bool HasQualityAllowed(int qualityId)
        {
            bool? result = base.InnerList.Find(x => x.Quality.Id.Equals(qualityId))?.Allowed;
            if (!result.HasValue)
                result = false;

            return result.Value;
        }
        public void Sort() => base.InnerList.Sort();
        public void Sort(IComparer<AllowedQuality> comparer) => base.InnerList.Sort(comparer);
        public AllowedQuality[] ToArray() => base.InnerList.ToArray();

        #endregion

        public static explicit operator AllowedQualityCollection(List<Quality> qualities) => 
            new AllowedQualityCollection(qualities.Select(x => AllowedQuality.FromQuality(x, false)));
    }
}
