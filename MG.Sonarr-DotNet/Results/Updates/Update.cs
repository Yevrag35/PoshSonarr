using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class that defines a response from the "/update" endpoint.
    /// </summary>
    public class Update : BaseResult
    {
        private const string CHANGES = "changes";
        private const string FIXED = "fixed";
        private const string NEW = "new";

        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        public string Branch { get; set; }
        public IEnumerable<string> ChangesFixed { get; private set; }
        public IEnumerable<string> ChangesNew { get; private set; }
        public string FileName { get; set; }
        public string Hash { get; set; }
        public bool Installed { get; set; }
        public bool Installable { get; set; }
        public bool Latest { get; set; }
        public Uri Url { get; set; }

        [OnDeserialized]
        private void OnDeserialization(StreamingContext streamingContext)
        {
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
