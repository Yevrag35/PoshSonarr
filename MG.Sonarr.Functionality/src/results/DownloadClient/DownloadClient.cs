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
    public class DownloadClient : Provider, IGetEndpoint
    {
        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonProperty("enable")]
        public bool IsEnabled { get; set; }

        [JsonIgnore]
        public override ProviderMessage Message { get; protected private set; }

        [JsonProperty("protocol")]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public DownloadProtocol Protocol { get; private set; }

        public string GetEndpoint() => "/downloadclient";
    }
}