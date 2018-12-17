using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class StatusResult : SonarrResult
    {
        internal override string[] SkipThese => new string[2] { "BuildTimeUtc", "UrlBase" };

        public string Version { get; internal set; }
        public string Branch { get; internal set; }
        public string StartupPath { get; internal set; }
        public string Authentication { get; internal set; }
        public string SqliteVersion { get; internal set; }
        public string AppData { get; internal set; }
        public bool IsProduction { get; internal set; }
        public bool IsDebug { get; internal set; }
        public bool IsUserInteractive { get; internal set; }
        public string RuntimeVersion { get; internal set; }
        public string RuntimeName { get; internal set; }
        public string OSName { get; internal set; }
        public string OSVersion { get; internal set; }
        public bool IsMono { get; internal set; }
        public bool IsMonoRuntime { get; internal set; }
        public bool IsAdmin { get; internal set; }
        public DateTime? BuildTime { get; internal set; }
        public DateTime? BuildTimeUtc { get; internal set; }
        public bool IsLinux { get; internal set; }
        public bool IsWindows { get; internal set; }
        public Uri UrlBase => new Uri(SonarrServiceContext.BaseUrl, UriKind.Absolute);

        public StatusResult() : base() { }
    }
}
