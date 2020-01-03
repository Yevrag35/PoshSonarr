using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// A download client set in Sonarr.
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class DownloadClient : Provider
    {
        private const string EP = "/downloadclient";

        [JsonProperty("id")]
        public int ClientId { get; private set; }

        [JsonProperty("enable")]
        public bool IsEnabled { get; set; }

        [JsonProperty("protocol")]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public DownloadProtocol Protocol { get; set; }

        public sealed override string GetEndpoint() => EP;
    }
}