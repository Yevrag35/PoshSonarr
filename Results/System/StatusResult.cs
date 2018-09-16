using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class StatusResult : SonarrResult
    {
        private string _version;
        private string _branch;
        private string _startupPath;
        private string _authentication;
        private string _sqliteVersion;
        private string _appData;
        private bool _isProduction;
        private bool _isDebug;
        private bool _isUserInteractive;
        private string _runtimeVersion;
        private string _runtimeName;
        private string _osName;
        private string _osVersion;
        private bool _isMono;
        private bool _isMonoRuntime;
        private bool _isAdmin;
        private DateTime _buildTime;
        private string _urlBase;
        private bool _isLinux;
        private bool _isWindows;

        public Version Version => Version.Parse(_version);
        public string Branch => _branch;
        public string StartupPath => _startupPath;
        public string Authentication => _authentication;
        public Version SqliteVersion => Version.Parse(_sqliteVersion);
        public string AppData => _appData;
        public bool IsProduction => _isProduction;
        public bool IsDebug => _isDebug;
        public bool IsUserInteractive => _isUserInteractive;
        public Version RuntimeVersion => Version.Parse(_runtimeVersion);
        public string RuntimeName => _runtimeName;
        public string OSName => _osName;
        public Version OSVersion => Version.Parse(_osVersion);
        public bool IsMono => _isMono;
        public bool IsMonoRuntime => _isMonoRuntime;
        public bool IsAdmin => _isAdmin;
        public DateTime BuildTime => _buildTime.ToLocalTime();
        public DateTime BuildTimeUtc => _buildTime;
        public bool IsLinux => _isLinux;
        public bool IsWindows => _isWindows;
        public Uri UrlBase => string.IsNullOrEmpty(_urlBase) ?
            (Uri)SonarrServiceContext.Value :
            new Uri(_urlBase, UriKind.RelativeOrAbsolute);
            

        private protected StatusResult(IDictionary dict) => MatchResultsToProperties(dict);

        public static explicit operator StatusResult(Dictionary<object, object> dict) =>
            new StatusResult(dict);
        public static explicit operator StatusResult(Dictionary<string, object> dict) =>
            new StatusResult(dict);
    }
}
