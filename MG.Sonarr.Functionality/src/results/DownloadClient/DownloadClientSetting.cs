using MG.Sonarr.Functionality;
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
    #region DOWNLOAD CLIENT SETTING

    /// <summary>
    /// Represents a setting set by download clients.
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn, MissingMemberHandling = MissingMemberHandling.Ignore, ItemNullValueHandling = NullValueHandling.Ignore)]
    public class DownloadClientSetting : BaseResult
    {
        #region PROPERTIES

        [JsonProperty("advanced")]
        public bool Advanced { get; set; }

        [JsonProperty("value")]
        public object BackendValue { get; private set; }

        [JsonProperty("helpText")]
        public string HelpText { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("order")]
        public int Order { get; set; }

        [JsonProperty("selectionOptions")]
        public SelectOptions[] SelectOptions { get; set; }

        [JsonProperty("type")]
        public FieldType Type { get; set; }

        public object Value { get; private set; }

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

    #region CLIENT SETTING COLLECTION

    [Serializable]
    public class DownloadClientSettingCollection : BaseResult, ICollection<DownloadClientSetting>
    {
        #region FIELDS/CONSTANTS
        private const int DEFAULT_SETTING_COUNT = 8;
        private List<DownloadClientSetting> _list;

        #endregion

        #region INDEXERS
        public DownloadClientSetting this[int index] => _list[index];
        public DownloadClientSetting this[string settingName] => _list.Find(x => x.Name.Equals(settingName, StringComparison.CurrentCultureIgnoreCase));

        #endregion

        #region PROPERTIES
        public int Count => _list.Count;
        bool ICollection<DownloadClientSetting>.IsReadOnly => false;

        #endregion

        #region CONSTRUCTORS
        public DownloadClientSettingCollection() => _list = new List<DownloadClientSetting>(DEFAULT_SETTING_COUNT);
        internal DownloadClientSettingCollection(int capacity) => _list = new List<DownloadClientSetting>(capacity);
        internal DownloadClientSettingCollection(IEnumerable<DownloadClientSetting> items) => _list = new List<DownloadClientSetting>(items);

        #endregion

        #region PUBLIC METHODS
        void ICollection<DownloadClientSetting>.Add(DownloadClientSetting item)
        {
            if (_list.Exists(x => x.Name.Equals(item.Name, StringComparison.CurrentCultureIgnoreCase)))
                throw new ArgumentException(string.Format("This collection already contains a setting with the name \"{0}\".", item.Name));

            _list.Add(item);
        }
        void ICollection<DownloadClientSetting>.Clear() => _list.Clear();
        bool ICollection<DownloadClientSetting>.Contains(DownloadClientSetting item) => 
            _list.Exists(x => x.Name.Equals(item.Name, StringComparison.InvariantCultureIgnoreCase));
        void ICollection<DownloadClientSetting>.CopyTo(DownloadClientSetting[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public bool Exists(Predicate<DownloadClientSetting> match) => _list.Exists(match);
        public IEnumerator<DownloadClientSetting> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((ICollection<DownloadClientSetting>)_list).GetEnumerator();
        public DownloadClientSettingCollection GetSettingByType(params FieldType[] types)
        {
            return new DownloadClientSettingCollection(_list.FindAll(x => types.Contains(x.Type)));
        }
        bool ICollection<DownloadClientSetting>.Remove(DownloadClientSetting item) => _list.Remove(item);
        public DownloadClientSetting[] ToArray() => _list.ToArray();
        public bool TrueForAll(Predicate<DownloadClientSetting> match) => _list.TrueForAll(match);

        #endregion

        #region BACKEND/PRIVATE METHODS


        #endregion
    }

    #endregion
}