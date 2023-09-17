using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    public abstract class IndexerBase : ProviderBase
    {
        [JsonProperty("protocol")]
        //[JsonConverter(typeof(SonarrStringEnumConverter))]
        public string Protocol { get; protected private set; } = string.Empty;

        [JsonProperty("enableRss")]
        public virtual bool RssEnabled { get; set; }

        [JsonProperty("supportsRss")]
        public bool RssSupported { get; protected private set; }

        [JsonProperty("enableSearch")]
        public virtual bool SearchEnabled { get; set; }

        [JsonProperty("supportsSearch")]
        public bool SearchSupported { get; protected private set; }
    }
}
