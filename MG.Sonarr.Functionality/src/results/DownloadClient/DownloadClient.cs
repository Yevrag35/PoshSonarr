using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// A download client set in Sonarr.
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class DownloadClient : ProviderBase, IComparable<DownloadClient>, IGetEndpoint
    {
        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonProperty("enable")]
        public bool IsEnabled { get; set; }

        [JsonProperty("protocol")]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public DownloadProtocol Protocol { get; private set; }

        public int CompareTo(DownloadClient other) => this.Id.CompareTo(other.Id);
        public string GetEndpoint() => "/downloadclient";
    }
}