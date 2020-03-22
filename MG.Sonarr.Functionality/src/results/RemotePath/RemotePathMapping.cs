using Newtonsoft.Json;
using System;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class RemotePathMapping : BaseResult, IComparable<RemotePathMapping>
    {
        [JsonProperty("host", Order = 1)]
        public string Host { get; set; }

        [JsonProperty("localPath", Order = 3)]
        public string LocalPath { get; set; }

        [JsonProperty("id", Order = 4)]
        public int MappingId { get; private set; }

        [JsonProperty("remotePath", Order = 2)]
        public string RemotePath { get; set; }

        public int CompareTo(RemotePathMapping other) => this.MappingId.CompareTo(other.MappingId);

        public static RemotePathMapping FormatNew(string host, string localPath, int mappingId, string remotePath)
        {
            return new RemotePathMapping
            {
                Host = host,
                LocalPath = localPath,
                MappingId = mappingId,
                RemotePath = remotePath
            };
        }
    }
}
