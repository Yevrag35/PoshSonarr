using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    #region FIELD

    /// <summary>
    /// Represents a setting set by a provider resource.
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn, MissingMemberHandling = MissingMemberHandling.Ignore, ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Field : BaseResult
    {
        #region PROPERTIES
        [JsonProperty("value")]
        public object BackendValue { get; private set; }

        [JsonProperty("helpText")]
        public string HelpText { get; private set; }

        [JsonProperty("advanced")]
        public bool IsAdvanced { get; private set; }

        [JsonProperty("label")]
        public string Label { get; private set; }

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("order")]
        public int Order { get; private set; }

        [JsonProperty("selectionOptions")]
        public SelectOptions[] SelectOptions { get; private set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public FieldType Type { get; private set; }

        public object Value { get; set; }

        #endregion

        #region ONDESERIALIZED

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (this.SelectOptions != null && this.BackendValue is long longVal)
            {
                int intVal = Convert.ToInt32(longVal);
                for (int i = 0; i < this.SelectOptions.Length; i++)
                {
                    SelectOptions so = this.SelectOptions[i];
                    if (intVal == so.Value)
                    {
                        this.Value = so.Name;
                        break;
                    }
                }
            }
            else
                this.Value = this.BackendValue;
        }

        #endregion
    }

    #endregion

    #region SELECT OPTIONS

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class SelectOptions : BaseResult
    {
        [JsonProperty("name")]
        public string Name { get; private set; }
        
        [JsonProperty("value")]
        public int Value { get; set; }
    }

    #endregion

    #region FIELD COLLECTION

    [Serializable]
    public class FieldCollection : BaseResult, IEnumerable<Field>
    {
        #region FIELDS/CONSTANTS
        private const int DEFAULT_SETTING_COUNT = 8;
        private List<Field> _list;

        #endregion

        #region INDEXERS
        public Field this[int index] => index != -1 ? _list[index] : _list.LastOrDefault();
        public Field this[string settingName] => _list.Find(x => x.Name.Equals(settingName, StringComparison.CurrentCultureIgnoreCase));

        #endregion

        #region PROPERTIES
        public int Count => _list.Count;

        #endregion

        #region CONSTRUCTORS
        public FieldCollection() => _list = new List<Field>(DEFAULT_SETTING_COUNT);
        internal FieldCollection(int capacity) => _list = new List<Field>(capacity);
        public FieldCollection(IEnumerable<Field> items) => _list = new List<Field>(items);

        #endregion

        #region PUBLIC METHODS
        //void ICollection<Field>.Add(Field item)
        //{
        //    if (_list.Exists(x => x.Name.Equals(item.Name, StringComparison.CurrentCultureIgnoreCase)))
        //        throw new ArgumentException(string.Format("This collection already contains a setting with the name \"{0}\".", item.Name));

        //    _list.Add(item);
        //}
        //void ICollection<Field>.Clear() => _list.Clear();
        //bool ICollection<Field>.Contains(Field item) => 
        //    _list.Exists(x => x.Name.Equals(item.Name, StringComparison.InvariantCultureIgnoreCase));
        //void ICollection<Field>.CopyTo(Field[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        //public bool Exists(Predicate<Field> match) => _list.Exists(match);
        public IEnumerator<Field> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((ICollection<Field>)_list).GetEnumerator();
        public FieldCollection GetSettingByType(params FieldType[] types)
        {
            return new FieldCollection(_list.FindAll(x => types.Contains(x.Type)));
        }
        //bool ICollection<Field>.Remove(Field item) => _list.Remove(item);
        public Field[] ToArray() => _list.ToArray();
        public bool TrueForAll(Predicate<Field> match) => _list.TrueForAll(match);

        #endregion
    }

    #endregion
}