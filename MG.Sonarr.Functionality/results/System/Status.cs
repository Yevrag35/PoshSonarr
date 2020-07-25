using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// <para type="description">The class that defines a response from the "/system/status" endpoint.</para>
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Status : BaseResult
    {
        [JsonProperty("appData")]
        public string AppData { get; private set; }

        [JsonProperty("authentication")]
        public string Authentication { get; private set; }

        [JsonProperty("buildTime")]
        public DateTime? BuildTime { get; private set; }

        [JsonProperty("branch")]
        public string Branch { get; private set; }

        [JsonProperty("isAdmin")]
        public bool IsAdmin { get; private set; }

        [JsonProperty("isDebug")]
        public bool IsDebug { get; private set; }

        [JsonProperty("isLinux")]
        public bool IsLinux { get; private set; }

        [JsonProperty("isMono")]
        public bool IsMono { get; private set; }

        [JsonProperty("isMonoRuntime")]
        public bool IsMonoRuntime { get; private set; }

        [JsonProperty("isProduction")]
        public bool IsProduction { get; private set; }

        [JsonProperty("isUserInteractive")]
        public bool IsUserInteractive { get; private set; }

        [JsonProperty("isWindows")]
        public bool IsWindows { get; private set; }

        [JsonProperty("osName")]
        public string OSName { get; private set; }

        [JsonProperty("osVersion")]
        public string OSVersion { get; private set; }

        [JsonProperty("runtimeName")]
        public string RuntimeName { get; private set; }

        [JsonProperty("runtimeVersion")]
        public string RuntimeVersion { get; private set; }

        [JsonProperty("sqliteVersion")]
        public string SqliteVersion { get; private set; }

        [JsonProperty("startupPath")]
        public string StartupPath { get; private set; }
        /// <summary>
        /// The specified url base used for reverse proxy purposes.
        /// </summary>
        [JsonProperty("urlBase")]
        public Uri UrlBase { get; private set; }

        [JsonProperty("version")]
        public string Version { get; private set; }
    }
}
