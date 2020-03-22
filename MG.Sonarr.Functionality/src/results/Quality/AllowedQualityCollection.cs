using System;
using System.Collections;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    [Serializable]
    public class AllowedQualityCollection : ResultCollectionBase<AllowedQuality>
    {
        #region INDEXERS
        public AllowedQuality this[int index] => base.InnerList[index];

        public AllowedQuality this[string qualityName] => this.Find(x => x.Quality.Name.Equals(qualityName, StringComparison.CurrentCultureIgnoreCase));

        #endregion

        #region CONSTRUCTORS
        public AllowedQualityCollection() : this(14) { }
        public AllowedQualityCollection(int capacity) : base(capacity) { }
        public AllowedQualityCollection(IEnumerable<AllowedQuality> qualityItems) : base(qualityItems) { }

        #endregion

        #region METHODS
        public void Add(AllowedQuality qi)
        {
            if (base.InnerList.Exists(x => x.Quality.Id.Equals(qi.Quality.Id)))
                throw new ArgumentException(string.Format("The QualityDefinition (Id: {0}) has already been added.", qi.Quality.Id));

            else
                base.InnerList.Add(qi);
        }
        public void AddFromQuality(Quality quality, bool isAllowed) => this.Add(new AllowedQuality
        {
            Allowed = isAllowed,
            Quality = quality
        });
        //public void Clear() => base.InnerList.Clear();
        public bool ContainsQualityById(int qualityId) => base.InnerList.Exists(x => x.Quality.Id.Equals(qualityId));
        public bool ContainsQualityByName(string qualityName)
        {
            StringComparison comparison = StringComparison.CurrentCultureIgnoreCase;
            return base.InnerList.Exists(x => x.Quality.Name.Equals(qualityName, comparison));
        }
        internal int FindIndex(Predicate<AllowedQuality> match) => base.InnerList.FindIndex(match);
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
        //internal bool Remove(AllowedQuality qualityDefinition) => base.InnerList.Remove(qualityDefinition);
        public void Sort() => base.InnerList.Sort();
        public void Sort(IComparer<AllowedQuality> comparer) => base.InnerList.Sort(comparer);
        public AllowedQuality[] ToArray() => base.InnerList.ToArray();

        #endregion
    }
}
