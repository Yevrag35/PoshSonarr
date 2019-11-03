using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MG.Sonarr.Results
{
    public class QualityItemCollection : BaseResult, IEnumerable<QualityItem>
    {
        #region FIELDS/CONSTANTS
        private List<QualityItem> _list;

        #endregion

        #region INDEXERS
        public QualityItem this[int index] => _list[index];

        #endregion

        #region PROPERTIES
        public int Count => _list.Count;

        #endregion

        #region CONSTRUCTORS
        public QualityItemCollection() => _list = new List<QualityItem>();
        public QualityItemCollection(int capacity) => _list = new List<QualityItem>(capacity);
        public QualityItemCollection(IEnumerable<QualityItem> qualityItems) => _list = new List<QualityItem>(qualityItems);

        #endregion

        #region METHODS
        public void Add(QualityItem qi) => _list.Add(qi);

        public IEnumerator<QualityItem> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        #endregion
    }
}