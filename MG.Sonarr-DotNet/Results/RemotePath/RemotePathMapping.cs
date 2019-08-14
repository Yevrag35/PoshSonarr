using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    public class RemotePathMapping : BaseResult
    {
        public string Host { get; set; }
        [JsonProperty("id")]
        public int MappingId { get; set; }
        public string LocalPath { get; set; }
        public string RemotePath { get; set; }
    }
}
