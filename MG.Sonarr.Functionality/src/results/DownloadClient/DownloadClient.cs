using MG.Sonarr.Functionality;
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
    public class DownloadClient : BaseResult
    {
        [JsonProperty("id")]
        public int ClientId { get; private set; }

        [JsonProperty("fields")]
        public DownloadClientSettingCollection Config { get; private set; }

        [JsonProperty("configContract")]
        public string ConfigContract { get; private set; }

        [JsonProperty("implementation")]
        public string Implementation { get; private set; }

        [JsonProperty("implementationName")]
        public string ImplementationName { get; private set; }

        [JsonProperty("infoLink")]
        public Uri InfoLink { get; set; }

        [JsonProperty("enable")]
        public bool IsEnabled { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("protocol")]
        public DownloadProtocol Protocol { get; set; }
    }
}