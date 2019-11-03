using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    public class Restriction : BaseResult
    {
        private const string COMMA = ",";
        private const string IGNORED = "ignored";
        private const string REQUIRED = "required";
        private static readonly string[] COMMA_ARR = new string[1] { COMMA };

        private bool ContainsIgnored => _additionalData != null && _additionalData.ContainsKey(IGNORED);
        private bool ContainsRequired => _additionalData != null && _additionalData.ContainsKey(REQUIRED);

        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        [JsonIgnore]
        public JsonStringCollection Ignored { get; private set; }
        [JsonIgnore]
        public JsonStringCollection Required { get; private set; }
        [JsonProperty("id")]
        public int RestrictionId { get; set; }
        public HashSet<int> Tags { get; set; }

        public Restriction()
        {
            _additionalData = new Dictionary<string, JToken>();
            this.Ignored = new JsonStringCollection();
            this.Required = new JsonStringCollection();
        }

        public Restriction(IDictionary<string, object> parameters)
        {
            _additionalData = new Dictionary<string, JToken>();
            this.Tags = new HashSet<int>();
            foreach (var kvp in parameters)
            {
                if (kvp.Key.Equals("IgnoredTerms") && kvp.Value is string[] igTerms)
                {
                    this.Ignored = igTerms;
                }
                else if (kvp.Key.Equals("RequiredTerms") && kvp.Value is string[] reqTerms)
                {
                    this.Required = reqTerms;
                }
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (this.ContainsIgnored)
            {
                JToken ign = _additionalData[IGNORED];
                if (ign != null)
                    this.Ignored.Add(ign.ToObject<string>().Split(COMMA_ARR, StringSplitOptions.RemoveEmptyEntries));
            }

            if (this.ContainsRequired)
            {
                JToken req = _additionalData[REQUIRED];
                if (req != null)
                    this.Required.Add(req.ToObject<string>().Split(COMMA_ARR, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (this.ContainsIgnored || (!this.ContainsIgnored && this.Ignored.Count > 0))
            {
                string ignoredStr = this.Ignored.ToJson();

                if (!this.ContainsIgnored)
                    _additionalData.Add(IGNORED, ignoredStr);

                else if (!_additionalData[IGNORED].ToObject<string>().Equals(ignoredStr))
                {
                    _additionalData.Remove(IGNORED);
                    _additionalData.Add(IGNORED, ignoredStr);
                }
            }

            if (this.ContainsRequired || (!this.ContainsRequired && this.Required.Count > 0))
            {
                string requiredStr = this.Required.ToJson();
                if (!this.ContainsRequired)
                    _additionalData.Add(REQUIRED, requiredStr);

                else if (!_additionalData[REQUIRED].ToObject<string>().Equals(requiredStr))
                {
                    _additionalData.Remove(REQUIRED);
                    _additionalData.Add(REQUIRED, requiredStr);
                }
            }
        }
    }
}
