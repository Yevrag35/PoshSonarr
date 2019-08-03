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
    public class DownloadClient : BaseResult
    {
        [JsonProperty("id")]
        public int ClientId { get; set; }
        [JsonProperty("fields")]
        public DownloadClientSettingCollection Config { get; set; }
        public string ConfigContract { get; set; }
        public string Implementation { get; set; }
        public string ImplementationName { get; set; }
        public Uri InfoLink { get; set; }
        [JsonProperty("enable")]
        public bool IsEnabled { get; set; }
        public string Name { get; set; }
        public DownloadProtocol Protocol { get; set; }
    }
}