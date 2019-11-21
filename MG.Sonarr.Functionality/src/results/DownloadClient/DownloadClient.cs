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
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class DownloadClient : BaseResult
    {
        [JsonProperty("id")]
        public int ClientId { get; set; }

        [JsonProperty("fields")]
        public DownloadClientSettingCollection Config { get; set; }

        [JsonProperty("configContract")]
        public string ConfigContract { get; set; }

        [JsonProperty("implementation")]
        public string Implementation { get; set; }

        [JsonProperty("implementationName")]
        public string ImplementationName { get; set; }

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