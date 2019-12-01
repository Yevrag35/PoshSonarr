using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class that defines a response from the "/update" endpoint.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Update : BaseResult, IComparable<Update>
    {
        private const string CHANGES = "changes";
        private const string FIXED = "fixed";
        private const string NEW = "new";

        private const string REGEX = @"\D{1,}\.\D{1,}\.((?:\d|\.){1,})\.";

        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        [JsonProperty("branch")]
        public string Branch { get; private set; }

        [JsonIgnore]
        public IEnumerable<string> ChangesFixed { get; private set; }

        [JsonIgnore]
        public IEnumerable<string> ChangesNew { get; private set; }

        [JsonProperty("fileName")]
        public string FileName { get; private set; }

        [JsonProperty("hash")]
        public string Hash { get; private set; }

        [JsonProperty("installed")]
        public bool IsInstalled { get; private set; }

        [JsonProperty("installable")]
        public bool IsInstallable { get; private set; }

        [JsonProperty("latest")]
        public bool Latest { get; private set; }

        [JsonProperty("url")]
        public Uri Url { get; private set; }

        [JsonProperty("version")]
        public Version Version { get; private set; }

        public int CompareTo(Update other) => this.Version.CompareTo(other.Version);

        [OnDeserialized]
        private void OnDeserialization(StreamingContext streamingContext)
        {
            if (!string.IsNullOrEmpty(this.FileName))
            {
                Match match = Regex.Match(this.FileName, REGEX);
                if (match.Success)
                {
                    this.Version = Version.Parse(match.Groups[1].Value);
                }
            }

            if (_additionalData.ContainsKey(CHANGES))
            {
                JToken changes = _additionalData[CHANGES];
                JToken fxed = changes.SelectToken("$." + FIXED);
                if (fxed != null)
                {
                    this.ChangesFixed = fxed.Values<string>();
                }
                JToken nw = changes.SelectToken("$." + NEW);
                if (nw != null)
                {
                    this.ChangesNew = nw.Values<string>();
                }
            }
        }
    }
}
