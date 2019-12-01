using Newtonsoft.Json;
using System;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class RemotePathMapping : BaseResult, IComparable<RemotePathMapping>
    {
        [JsonProperty("host")]
        public string Host { get; set; }

        [JsonProperty("localPath")]
        public string LocalPath { get; set; }

        [JsonProperty("id")]
        public int MappingId { get; set; }

        [JsonProperty("remotePath")]
        public string RemotePath { get; set; }

        public int CompareTo(RemotePathMapping other) => this.MappingId.CompareTo(other.MappingId);
    }
}
