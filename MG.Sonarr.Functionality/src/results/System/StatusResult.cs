using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// <para type="description">The class that defines a response from the "/system/status" endpoint.</para>
    /// </summary>
    public class SonarrStatusResult : BaseResult
    {
        public string AppData { get; set; }
        public string Authentication { get; set; }
        public DateTime? BuildTime { get; set; }
        public string Branch { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsDebug { get; set; }
        public bool IsLinux { get; set; }
        public bool IsMono { get; set; }
        public bool IsMonoRuntime { get; set; }
        public bool IsProduction { get; set; }
        public bool IsUserInteractive { get; set; }
        public bool IsWindows { get; set; }
        public string OSName { get; set; }
        public string OSVersion { get; set; }
        public string RuntimeName { get; set; }
        public string RuntimeVersion { get; set; }
        public string SqliteVersion { get; set; }
        public string StartupPath { get; set; }
        /// <summary>
        /// The specified url base used for reverse proxy purposes.
        /// </summary>
        public Uri UrlBase { get; set; }
        public string Version { get; set; }
    }
}
