using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Http;
using System.Reflection;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommunications.Connect, "Instance", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByServerName")]
    [CmdletBinding(PositionalBinding = false)]
    [Alias("Connect-", "conson")]
    public class ConnectInstance : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string SLASH_STR = "/";
        private static readonly char SLASH = char.Parse(SLASH_STR);

        private bool _allowRedirect;
        private bool _noApiPrefix;
        private bool _passThru;
        private bool _proxyBypass;
        private bool _skipCert;
        private bool _useSsl;
        private const string URL_FORMAT = "{0}://{1}:{2}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByServerName")]
        [Alias("Server", "ServerName", "HostName")]
        public string SonarrServerName = "localhost";

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySonarrUrl")]
        [Alias("Url")]
        public Uri SonarrUrl { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = "ByServerName")]
        [Alias("Port")]
        public int PortNumber = 8989;

        [Parameter(Mandatory = false, ParameterSetName = "ByServerName")]
        [Alias("CustomUriBase", "UriBase")]
        public string ReverseProxyUriBase { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = "ByServerName")]
        public SwitchParameter UseSSL
        {
            get => _useSsl;
            set => _useSsl = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter SkipCertificateCheck
        {
            get => _skipCert;
            set => _skipCert = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter AllowRedirects
        {
            get => _allowRedirect;
            set => _allowRedirect = value;
        }

        [Parameter(Mandatory = false)]
        public string Proxy { get; set; }

        [Parameter(Mandatory = false)]
        public ProxyCredential ProxyCredential { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter ProxyBypassOnLocal
        {
            get => _proxyBypass;
            set => _proxyBypass = value;
        }

        [Parameter(Mandatory = true, HelpMessage = "Can be retrieved from your Sonarr website (Settings => General => Security), or in the \"Config.xml\" file in the AppData directory.")]
        [Alias("key")]
        public ApiKey ApiKey { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter NoApiPrefix
        {
            get => _noApiPrefix;
            set => _noApiPrefix = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru
        {
            get => _passThru;
            set => _passThru = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            if (_allowRedirect)
                base.WriteWarning(BaseSonarrHttpException.CAUTION + BaseSonarrHttpException.HOW_CAUTION);

            Context.UriBase = null;
        }

        protected override void ProcessRecord()
        {
            UriBuilder url = this.ParameterSetName == "ByServerName"
                ? FormatUri(this.SonarrServerName, this.PortNumber, this.ReverseProxyUriBase, _useSsl, _noApiPrefix)
                : new UriBuilder(this.SonarrUrl);

            this.CheckCertificateValidity();

            ApiCaller apiCaller = this.MyInvocation.BoundParameters.ContainsKey("Proxy")
                ? NewApiCaller(this.ApiKey, url, _allowRedirect, this.Proxy, this.ProxyCredential, _proxyBypass)
                : NewApiCaller(this.ApiKey, url, _allowRedirect);

            Context.ApiCaller = apiCaller;
            Context.UriBase = url.Path;

            if (_passThru)
            {
                string status = this.GetStatusString(Context.ApiCaller);
                SonarrStatusResult sr = this.GetStatusResult(status);
                if (sr != null)
                    base.WriteObject(sr);
            }
        }

        #endregion

        #region PRIVATE/BACKEND METHODS
        private void CheckCertificateValidity()
        {
            if (_skipCert)
            {
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            }
        }

        public static UriBuilder FormatUri(string serverName, int portNumber, string reverseProxyUriBase, bool useSsl, bool noApiPrefix)
        {
            string scheme = !useSsl
                    ? "http"
                    : "https";

            var url = new UriBuilder()
            {
                Scheme = scheme,
                Host = serverName,
                Port = portNumber
            };
            if (!noApiPrefix)
            {
                url.Path = "/api";
            }

            if (!string.IsNullOrEmpty(reverseProxyUriBase))
            {
                reverseProxyUriBase = reverseProxyUriBase.Trim(SLASH);
                if (reverseProxyUriBase.IndexOf("/api", StringComparison.CurrentCultureIgnoreCase) >= 0 &&
                    url.Path.Contains("/api"))
                {
                    reverseProxyUriBase = reverseProxyUriBase.Replace("/api", string.Empty);
                }
                url.Path = reverseProxyUriBase + url.Path;
            }
            return url;
        }

        private string GetStatusString(ApiCaller caller)
        {
            string status = base.TrySonarrConnect();
            if (string.IsNullOrEmpty(status))
            {
                throw new NoSonarrResponseException();
            }
            return status;
            // Now call GetStatusResult();
        }

        private SonarrStatusResult GetStatusResult(string statusStr)
        {
            SonarrStatusResult sr = null;
            try
            {
                sr = SonarrHttp.ConvertToSonarrResult<SonarrStatusResult>(statusStr);
            }
            catch (Exception e)
            {
                base.WriteError(e, ErrorCategory.ParserError, statusStr);
            }
            return sr;
        }

        public static ApiCaller NewApiCaller(ApiKey apiKey, UriBuilder uriBuilder, bool allowRedirects)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = allowRedirects,
                UseDefaultCredentials = true
            };
            return new ApiCaller(handler, apiKey)
            {
                BaseAddress = new Uri(uriBuilder.Uri.GetLeftPart(UriPartial.Scheme | UriPartial.Authority))
            };
        }

        public static ApiCaller NewApiCaller(ApiKey apiKey, UriBuilder uriBuilder, bool allowRedirects, string proxy, ProxyCredential proxyCredential, bool proxyBypassLocal)
        {
            var wp = new WebProxy(proxy)
            {
                BypassProxyOnLocal = proxyBypassLocal,
            };

            if (proxyCredential != null)
                wp.Credentials = proxyCredential;

            else
                wp.UseDefaultCredentials = true;

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = allowRedirects,
                UseDefaultCredentials = true,
                Proxy = wp,
                UseProxy = true
            };
            return new ApiCaller(handler, apiKey)
            {
                BaseAddress = new Uri(uriBuilder.Uri.GetLeftPart(UriPartial.Scheme | UriPartial.Authority))
            };
        }

        #endregion
    }
}