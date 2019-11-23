using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class LogFile : BaseResult
    {
        [JsonProperty("contentsUrl")]
        public string ContentsUrl { get; set; }

        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonProperty("fileName")]
        public string FileName { get; set; }

        [JsonProperty("lastWriteTime")]
        public DateTime LastWriteTime { get; set; }

        [JsonProperty("id")]
        public int LogFileId { get; set; }

        internal class LogFileSortById : IComparer<LogFile>
        {
            public int Compare(LogFile x, LogFile y) => x.LogFileId.CompareTo(y.LogFileId);
        }
    }}
