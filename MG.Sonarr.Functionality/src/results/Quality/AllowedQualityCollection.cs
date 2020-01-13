using System;
using System.Collections;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    [Serializable]
    public class AllowedQualityCollection : BaseResult, ICollection<AllowedQuality>
    {
        #region FIELDS/CONSTANTS
        private List<AllowedQuality> _list;

        #endregion

        #region INDEXERS
        public AllowedQuality this[int index] => _list[index];

        #endregion

        #region PROPERTIES
        public int Count => _list.Count;
        bool ICollection<AllowedQuality>.IsReadOnly => ((ICollection<AllowedQuality>)_list).IsReadOnly;

        #endregion

        #region CONSTRUCTORS
        public AllowedQualityCollection() : this(14) { }
        public AllowedQualityCollection(int capacity) => _list = new List<AllowedQuality>(capacity);
        public AllowedQualityCollection(IEnumerable<AllowedQuality> qualityItems) => _list = new List<AllowedQuality>(qualityItems);

        #endregion

        #region METHODS
        public void Add(AllowedQuality qi)
        {
            if (_list.Exists(x => x.Quality.Id.Equals(qi.Quality.Id)))
                throw new ArgumentException(string.Format("The QualityDefinition (Id: {0}) has already been added.", qi.Quality.Id));

            else
                _list.Add(qi);
        }
        public void AddFromQuality(Quality quality, bool isAllowed) => this.Add(new AllowedQuality
        {
            Allowed = isAllowed,
            Quality = quality
        });
        public void Clear() => _list.Clear();
        public bool Contains(AllowedQuality qualityDefinition) => _list.Contains(qualityDefinition);
        public bool ContainsQualityById(int qualityId) => _list.Exists(x => x.Quality.Id.Equals(qualityId));
        public bool ContainsQualityByName(string qualityName, bool caseSensitive = false)
        {
            StringComparison comparison = StringComparison.CurrentCultureIgnoreCase;
            if (caseSensitive)
                comparison = StringComparison.CurrentCulture;

            return _list.Exists(x => x.Quality.Name.Equals(qualityName, comparison));
        }
        void ICollection<AllowedQuality>.CopyTo(AllowedQuality[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        //public AllowedQuality Find(Predicate<AllowedQuality> match) => _list.Find(match);
        public AllowedQualityCollection FindAll(Predicate<AllowedQuality> match) => new AllowedQualityCollection(_list.FindAll(match));
        public int FindIndex(Predicate<AllowedQuality> match) => _list.FindIndex(match);
        public AllowedQuality GetAllowedQualityByName(string qualityName, bool caseSensitive = false)
        {
            StringComparison comparison = StringComparison.CurrentCultureIgnoreCase;
            if (caseSensitive)
                comparison = StringComparison.CurrentCulture;

            return _list.Find(x => x.Quality.Name.Equals(qualityName, comparison));
        }
        public AllowedQuality GetAllowedQualityById(int qualityId) => _list.Find(x => x.Quality.Id.Equals(qualityId));
        public IEnumerator<AllowedQuality> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
        public bool HasQualityAllowed(int qualityId)
        {
            bool? result = _list.Find(x => x.Quality.Id.Equals(qualityId))?.Allowed;
            if (!result.HasValue)
                result = false;

            return result.Value;
        }
        public bool Remove(AllowedQuality qualityDefinition) => _list.Remove(qualityDefinition);
        public AllowedQuality[] ToArray() => _list.ToArray();
        public bool TrueForAll(Predicate<AllowedQuality> match) => _list.TrueForAll(match);

        #endregion
    }
}
