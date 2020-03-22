using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [Serializable]
    public class FieldCollection : ResultCollectionBase<Field>
    {
        #region FIELDS/CONSTANTS
        private const int DEFAULT_SETTING_COUNT = 8;

        #endregion

        #region INDEXERS
        public Field this[int index] => index != -1 ? base.InnerList[index] : base.InnerList.LastOrDefault();
        public Field this[string settingName] => base.InnerList.Find(x => x.Name.Equals(settingName, StringComparison.CurrentCultureIgnoreCase));

        #endregion

        #region CONSTRUCTORS
        public FieldCollection() : base(DEFAULT_SETTING_COUNT) { }
        internal FieldCollection(int capacity) : base(capacity) { }
        [JsonConstructor]
        internal FieldCollection(IEnumerable<Field> items) : base(items) { }

        #endregion

        #region PUBLIC METHODS
        public FieldCollection GetSettingByType(params FieldType[] types) => new FieldCollection(base.InnerList.FindAll(x => types.Contains(x.Type)));
        public Field[] ToArray() => base.InnerList.ToArray();

        #endregion
    }
}
