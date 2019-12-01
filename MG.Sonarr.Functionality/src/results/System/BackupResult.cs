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
    public class SonarrBackupResult : BaseResult, IComparable<SonarrBackupResult>
    {
        [JsonProperty("id")]
        public long BackupId { get; }

        [JsonProperty("name")]
        public string BackupName { get; }

        [JsonProperty("type")]
        public BackupType BackupType { get; }

        [JsonProperty("path")]
        public string Path { get; }

        [JsonProperty("time")]
        public DateTime? Time { get; }

        public int CompareTo(SonarrBackupResult other) => this.BackupId.CompareTo(other.BackupId);
    }
}
