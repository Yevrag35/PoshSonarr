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
using System.Security;

namespace MG.Sonarr.Results
{
    #region FIELD

    /// <summary>
    /// Represents a setting set by a provider resource.
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn, MissingMemberHandling = MissingMemberHandling.Ignore, ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Field : BaseResult, IField
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
        IEnumerable<ISelectOption> IField.SelectOptions => this.SelectOptions;

        [JsonProperty("type")]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public FieldType Type { get; private set; }

        public object Value { get; set; }

        [JsonConstructor]
        public Field() { }
        internal Field(IField field)
        {
            this.Name = field.Name;
            this.BackendValue = field.BackendValue;
            this.HelpText = field.HelpText;
            this.IsAdvanced = field.IsAdvanced;
            this.Label = field.Label;
            this.Order = field.Order;
            this.SelectOptions = field.SelectOptions.Select(x => new SelectOptions(x.Name, x.Value)).ToArray();
            this.Type = field.Type;
            this.Value = field.Value;
        }

        internal Field(int order, string name, string label, object value, FieldType type, bool isAdvanced, params SelectOptions[] selectOptions)
        {
            Order = order;
            Name = name;
            Label = label;
            BackendValue = value;
            Type = type;
            IsAdvanced = isAdvanced;
            this.SelectOptions = selectOptions;
        }

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

        public Type GetDotNetTypeFromFieldType()
        {
            switch (this.Type)
            {
                case FieldType.CheckBox:
                    return typeof(bool);

                case FieldType.Password:
                    return typeof(SecureString);

                default:
                    return typeof(string);
            }
        }

        public string GetLabelNoSpaces() => this.Label.Replace(" ", string.Empty);
    }

    #endregion

    #region SELECT OPTIONS

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class SelectOptions : BaseResult, ISelectOption
    {
        [JsonProperty("name")]
        public string Name { get; private set; }
        
        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonConstructor]
        internal SelectOptions(string name, int value)
        {
            this.Name = name;
            this.Value = value;
        }
    }

    #endregion
}