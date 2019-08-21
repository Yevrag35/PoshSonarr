using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    public class LogFile : BaseResult
    {
        public string ContentsUrl { get; set; }
        public string DownloadUrl { get; set; }
        public string FileName { get; set; }
        public DateTime LastWriteTime { get; set; }
        [JsonProperty("id")]
        public int LogFileId { get; set; }
    }

    internal class LogFileSortById : IComparer<LogFile>
    {
        public int Compare(LogFile x, LogFile y) => x.LogFileId.CompareTo(y.LogFileId);
    }
}
