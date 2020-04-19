using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    public class Indexer : ProviderBase, IComparable<Indexer>
    {
        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonProperty("protocol")]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public DownloadProtocol Protocol { get; private set; }

        [JsonProperty("enableRss")]
        public bool RssEnabled { get; set; }

        [JsonProperty("supportsRss")]
        public bool RssSupported { get; private set; }

        [JsonProperty("enableSearch")]
        public bool SearchEnabled { get; set; }

        [JsonProperty("supportsSearch")]
        public bool SearchSupported { get; private set; }

        public int CompareTo(Indexer other) => this.Id.CompareTo(other.Id);
    }
}
