using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MG.Sonarr.Results
{
    [Serializable]
    public class QualityDefinitionCollection : ResultCollectionBase<QualityDefinition>
    {
        #region INDEXERS
        public QualityDefinition this[int index] => base.InnerList[index];

        #endregion

        #region CONSTRUCTORS
        public QualityDefinitionCollection() : this(14) { }
        public QualityDefinitionCollection(int capacity) : base(capacity) { }
        [JsonConstructor]
        internal QualityDefinitionCollection(IEnumerable<QualityDefinition> qualityItems)
            : base(qualityItems)
        {
        }

        #endregion

        #region METHODS
        //public void Add(QualityDefinition qi) => base.InnerList.Add(qi);

        #endregion
    }
}