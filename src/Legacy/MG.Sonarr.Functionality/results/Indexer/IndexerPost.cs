using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    public class IndexerPost : BaseResult
    {
        [JsonProperty("protocol")]
        //[JsonConverter(typeof(SonarrStringEnumConverter))]
        public string Protocol { get; private set; } = string.Empty;

        [JsonProperty("enableRss")]
        public bool RssEnabled { get; set; }

        [JsonProperty("supportsRss")]
        public bool RssSupported { get; private set; }

        [JsonProperty("enableSearch")]
        public bool SearchEnabled { get; set; }

        [JsonProperty("supportsSearch")]
        public bool SearchSupported { get; private set; }
    }
}
