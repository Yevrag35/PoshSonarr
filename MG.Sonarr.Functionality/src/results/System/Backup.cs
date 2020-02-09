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
    public class Backup : BaseResult, IComparable<Backup>
    {
        [JsonProperty("id")]
        public long Id { get; private set; }

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("path")]
        public string Path { get; private set; }

        [JsonProperty("time")]
        public DateTime? Time { get; private set; }

        [JsonProperty("type")]
        public BackupType Type { get; private set; }

        public int CompareTo(Backup other) => this.Time.GetValueOrDefault().CompareTo(other.Time.GetValueOrDefault());
    }
}
