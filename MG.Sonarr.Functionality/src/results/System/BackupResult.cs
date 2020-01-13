using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class that defines a response from the "/system/backup" endpoint.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class BackupResult : BaseResult, IComparable<BackupResult>
    {
        [JsonProperty("id")]
        public long BackupId { get; private set; }

        [JsonProperty("name")]
        public string BackupName { get; private set; }

        [JsonProperty("type")]
        public BackupType BackupType { get; private set; }

        [JsonProperty("path")]
        public string Path { get; private set; }

        [JsonProperty("time")]
        public DateTime? Time { get; private set; }

        public int CompareTo(BackupResult other) => this.BackupId.CompareTo(other.BackupId);
    }
}
