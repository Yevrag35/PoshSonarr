using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MG.Sonarr.Results
{
    [Serializable]
    public class QualityDefinitionCollection : BaseResult, ICollection<QualityDefinition>
    {
        #region FIELDS/CONSTANTS
        private List<QualityDefinition> _list;

        #endregion

        #region INDEXERS
        public QualityDefinition this[int index] => _list[index];

        #endregion

        #region PROPERTIES
        public int Count => _list.Count;
        bool ICollection<QualityDefinition>.IsReadOnly => ((ICollection<QualityDefinition>)_list).IsReadOnly;

        #endregion

        #region CONSTRUCTORS
        public QualityDefinitionCollection() : this(14) { }
        public QualityDefinitionCollection(int capacity) => _list = new List<QualityDefinition>(capacity);
        public QualityDefinitionCollection(IEnumerable<QualityDefinition> qualityItems) => _list = new List<QualityDefinition>(qualityItems);

        #endregion

        #region METHODS
        public void Add(QualityDefinition qi) => _list.Add(qi);
        public void Clear() => _list.Clear();
        public bool Contains(QualityDefinition qualityDefinition) => _list.Contains(qualityDefinition);
        void ICollection<QualityDefinition>.CopyTo(QualityDefinition[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public IEnumerator<QualityDefinition> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
        public bool Remove(QualityDefinition qualityDefinition) => _list.Remove(qualityDefinition);

        #endregion
    }
}