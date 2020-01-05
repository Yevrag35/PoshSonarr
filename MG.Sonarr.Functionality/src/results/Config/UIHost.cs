using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UIHost
    {
        #region JSON PROPERTIES
        [JsonProperty("id")]
        public int UIHostId { get; private set; }

        [JsonProperty("analyticsEnabled", Order = 7)]
        public bool AnalyticsEnabled { get; set; }

        [JsonProperty("authenticationMethod", Order = 6)]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public AuthenticationType AuthenticationMethod { get; set; }

        [JsonProperty("bindAddress", Order = 1)]
        public string BindAddress { get; set; }

        [JsonProperty("port", Order = 2)]
        public int Port { get; set; }

        [JsonProperty("sslPort", Order = 3)]
        public int SslPort { get; set; }

        [JsonProperty("enableSsl", Order = 4)]
        public bool EnableSsl { get; set; }

        [JsonProperty("launchBrowser", Order = 5)]
        public bool LaunchBrowser { get; set; }

        [JsonProperty("username", Order = 8)]
        public string Username { get; set; }

        [JsonProperty("password", Order = 9)]
        public string Password { get; set; }

        [JsonProperty("logLevel", Order = 10)]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public LogLevel LogLevel { get; set; }

        [JsonProperty("branch", Order = 11)]
        public string Branch { get; set; }

        [JsonProperty("apiKey", Order = 12)]
        public string ApiKey { get; private set; }

        [JsonProperty("sslCertHash", Order = 13)]
        public string SslCertHash { get; private set; }

        [JsonProperty("urlBase", Order = 14)]
        public string UrlBase { get; private set; }

        [JsonProperty("updateAutomatically", Order = 15)]
        public bool UpdateAutomatically { get; set; }

        [JsonProperty("updateMechanism", Order = 16)]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public UpdateMechanism UpdateMechanism { get; private set; }

        [JsonProperty("updateScriptPath", Order = 17)]
        public string UpdateScriptPath { get; private set; }

        [JsonProperty("proxyEnabled")]
        public bool ProxyEnabled { get; set; }

        [JsonProperty("proxyType")]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public ProxyType ProxyType { get; set; }

        [JsonProperty("proxyHostname")]
        public string ProxyHostname { get; set; }

        [JsonProperty("proxyPort")]
        public int ProxyPort { get; set; }

        [JsonProperty("proxyUserName")]
        public string ProxyUsername { get; set; }

        [JsonProperty("proxyPassword")]
        public string ProxyPassword { get; set; }

        [JsonProperty("proxyBypassFilter")]
        public string ProxyBypassFilter { get; set; }

        [JsonProperty("proxyBypassLocalAddresses")]
        public bool ProxyBypassLocalAddresses { get; set; }

        [JsonProperty("backupFolder")]
        public string BackupFolder { get; set; }

        [JsonProperty("backupInterval")]
        public int BackupInterval { get; set; }

        [JsonProperty("backupRetention")]
        public int BackupRetention { get; set; }

        #endregion
    }
}