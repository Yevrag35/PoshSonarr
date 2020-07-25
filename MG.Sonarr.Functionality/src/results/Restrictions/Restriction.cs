using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Client;
using MG.Sonarr.Functionality.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Restriction : BaseResult, ISupportsTagUpdate
    {
        private const string COMMA = ",";
        private const string IGNORED = "ignored";
        private const string REQUIRED = "required";
        private static readonly string[] COMMA_ARR = new string[1] { COMMA };
        private const string EP = "/restriction";

        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData { get; set; } = new Dictionary<string, JToken>();

        [JsonIgnore]
        internal bool ContainsIgnored => _additionalData.ContainsKey(IGNORED);
        [JsonIgnore]
        internal bool ContainsRequired => _additionalData.ContainsKey(REQUIRED);

        object ISupportsTagUpdate.Id => this.Id;

        [JsonIgnore]
        public JsonStringCollection Ignored { get; } = new JsonStringCollection();

        [JsonIgnore]
        public JsonStringCollection Required { get; } = new JsonStringCollection();

        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonProperty("tags")]
        public HashSet<int> Tags { get; set; } = new HashSet<int>();

        public Restriction() { }

        public Restriction(IDictionary<string, object> parameters)
        {
            foreach (KeyValuePair<string, object> kvp in parameters)
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

        public string GetEndpoint() => EP;

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
