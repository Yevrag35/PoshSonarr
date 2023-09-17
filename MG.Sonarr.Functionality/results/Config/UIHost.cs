using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UIHost : BaseResult
    {
        [JsonExtensionData]
        private IDictionary<string, object> _additional;

        #region JSON PROPERTIES
        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonProperty("analyticsEnabled", Order = 7)]
        public bool AnalyticsEnabled { get; set; }

        [JsonProperty("apiKey", Order = 12)]
        public string ApiKey { get; private set; }

        [JsonProperty("authenticationMethod", Order = 6)]
        //[JsonConverter(typeof(SonarrStringEnumConverter))]
        public string AuthenticationMethod { get; set; }

        [JsonProperty("bindAddress", Order = 1)]
        public string BindAddress { get; set; }

        [JsonProperty("branch", Order = 11)]
        public string Branch { get; set; }

        [JsonProperty("enableSsl", Order = 4)]
        public bool EnableSsl { get; set; }

        [JsonProperty("launchBrowser", Order = 5)]
        public bool LaunchBrowser { get; set; }

        [JsonProperty("logLevel", Order = 10)]
        public string LogLevel { get; set; } = string.Empty;

        [JsonProperty("password", Order = 9)]
        public string Password { get; set; }

        [JsonProperty("port", Order = 2)]
        public int Port { get; set; }

        [JsonProperty("proxyBypassFilter")]
        public string ProxyBypassFilter { get; set; }

        [JsonProperty("proxyBypassLocalAddresses")]
        public bool ProxyBypassLocalAddresses { get; set; }

        [JsonProperty("proxyEnabled")]
        public bool ProxyEnabled { get; set; }

        [JsonProperty("proxyHostname")]
        public string ProxyHostname { get; set; }

        [JsonProperty("proxyPassword")]
        public string ProxyPassword { get; set; }

        [JsonProperty("proxyPort")]
        public int ProxyPort { get; set; }

        [JsonProperty("proxyType")]
        //[JsonConverter(typeof(SonarrStringEnumConverter))]
        public string ProxyType { get; set; } = string.Empty;

        [JsonProperty("proxyUserName")]
        public string ProxyUsername { get; set; }

        [JsonProperty("sslCertHash", Order = 13)]
        public string SslCertHash { get; private set; }

        [JsonProperty("sslPort", Order = 3)]
        public int SslPort { get; set; }

        [JsonProperty("updateAutomatically", Order = 15)]
        public bool UpdateAutomatically { get; set; }

        [JsonProperty("updateMechanism", Order = 16)]
        public string UpdateMechanism { get; set; } = string.Empty;

        [JsonProperty("updateScriptPath", Order = 17)]
        public string UpdateScriptPath { get; set; }

        [JsonProperty("urlBase", Order = 14)]
        public string UrlBase { get; private set; }

        [JsonProperty("username", Order = 8)]
        public string Username { get; set; }

        #endregion

        public IDictionary<string, object> GetAdditionalData() => _additional;
    }
}