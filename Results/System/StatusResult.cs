using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class StatusResult : SonarrResult
    {
        //private string _version;
        //private string _branch;
        //private string _startupPath;
        //private string _authentication;
        //private string _sqliteVersion;
        //private string _appData;
        //private bool _isProduction;
        //private bool _isDebug;
        //private bool _isUserInteractive;
        //private string _runtimeVersion;
        //private string _runtimeName;
        //private string _osName;
        //private string _osVersion;
        //private bool _isMono;
        //private bool _isMonoRuntime;
        //private bool _isAdmin;
        //private DateTime? _buildTime;
        //private string _urlBase;
        //private bool _isLinux;
        //private bool _isWindows;

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
        //public Uri UrlBase => string.IsNullOrEmpty(_urlBase) ?
        //    (Uri)SonarrServiceContext.Value :
        //    new Uri(_urlBase, UriKind.RelativeOrAbsolute);


        //private protected StatusResult(IDictionary dict) => MatchResultsToProperties(dict);

        //public static explicit operator StatusResult(Dictionary<object, object> dict) =>
        //    new StatusResult(dict);
        //public static explicit operator StatusResult(Dictionary<string, object> dict) =>
        //    new StatusResult(dict);
    }
}
