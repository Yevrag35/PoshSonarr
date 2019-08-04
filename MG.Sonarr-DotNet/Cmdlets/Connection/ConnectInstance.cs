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
    /// <summary>
    ///     <para type="synopsis">Builds the connection context for subsequent cmdlets.</para>
    ///     <para type="description">Establishes a custom HttpClient context for use with all subsequent PoshSonarr cmdlets.  
    ///         The connection is created either via hostname/port/url base or by direct URL.  
    ///         The "/api" path is automatically appended unless the '-NoApiPrefix' parameter is used.  
    ///         If this command is not run first, all other cmdlets will throw an error.
    ///     </para>
    ///     <para type="link" uri="https://github.com/Yevrag35/PoshSonarr/wiki/Connect-SonarrInstance">Online version:</para>
    /// </summary>
    /// <example>
    ///     <para>Connect by 'HostName' and 'Port'</para>
    ///     <code>Connect-Sonarr -Server "MEDIASERVER" -ApiKey "xxxxxxxxxxxxxxxx" -PassThru</code>
    /// </example>
    [Cmdlet(VerbsCommunications.Connect, "Instance", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByServerName")]
    [CmdletBinding(PositionalBinding = false)]
    [Alias("Connect-", "conson")]
    [OutputType(typeof(SonarrStatusResult))]
    public partial class ConnectInstance : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string SLASH_STR = "/";
        private static readonly char SLASH = char.Parse(SLASH_STR);
        private static readonly char[] SLASH_API = new char[4]
        {
            SLASH, char.Parse("a"), char.Parse("p"), char.Parse("i")
        };

        private bool _allowRedirect;
        private bool _noApiPrefix;
        private bool _passThru;
        private bool _proxyBypass;
        private bool _skipCert;
        private bool _useSsl;
        private const string URL_FORMAT = "{0}://{1}:{2}";

        #endregion

        #region PARAMETERS
        /// <summary>
        /// <para type="description">The hostname of the Sonarr instance.</para>
        /// </summary>
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByServerName")]
        [Alias("HostName")]
        public string SonarrServerName = "localhost";

        /// <summary>
        /// <para type="description">Specifies the direct URL to the Sonarr instance; including any reverse proxy bases.  
        /// The "/api" path is automatically appended unless the '-NoApiPrefix' parameter is used.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySonarrUrl")]
        [Alias("Url")]
        public Uri SonarrUrl { get; set; }

        /// <summary>
        /// <para type="description">The port number for the Sonarr website.</para>
        /// </summary>
        [Parameter(Mandatory = false, ParameterSetName = "ByServerName")]
        public int PortNumber = 8989;

        /// <summary>
        /// <para type="description">Specifies a custom URL base for use with reverse proxies.  
        /// If you don't use a reverse proxy with Sonarr, then you don't need this parameter :)</para>
        /// </summary>
        [Parameter(Mandatory = false, ParameterSetName = "ByServerName")]
        public string ReverseProxyUriBase { get; set; }

        /// <summary>
        /// <para type="description">Indicates that connection should establish over an SSL connection when using the "ByServerName" parameter set.</para>
        /// </summary>
        [Parameter(Mandatory = false, ParameterSetName = "ByServerName")]
        public SwitchParameter UseSSL
        {
            get => _useSsl;
            set => _useSsl = value;
        }

        /// <summary>
        /// <para type="description">Specifies the HttpClient to ignore any certificate errors that may occur.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter SkipCertificateCheck
        {
            get => _skipCert;
            set => _skipCert = value;
        }

        /// <summary>
        /// <para type="description">Specifies the HttpClient to follow any HTTP redirects.  See the wiki article for more information: https://github.com/Yevrag35/PoshSonarr/wiki/Reverse-Proxy-Information</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter AllowRedirects
        {
            get => _allowRedirect;
            set => _allowRedirect = value;
        }

        /// <summary>
        /// <para type="description">Specifies a proxy URL that HttpClient must use.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string Proxy { get; set; }

        /// <summary>
        /// <para type="description">Specifies a set of credentials in order to access the proxy.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public ProxyCredential ProxyCredential { get; set; }

        /// <summary>
        /// <para type="description">Indicates that the proxy should be used on local connections.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter ProxyBypassOnLocal
        {
            get => _proxyBypass;
            set => _proxyBypass = value;
        }

        /// <summary>
        /// <para type="description">The API key to use for authentication.  The key is 32, all lower-case, alphanumeric characters.  
        /// The key can be retrieved from your Sonarr website (Settings => General => Security), or in the "Config.xml" file in the AppData directory.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "Can be retrieved from your Sonarr website (Settings => General => Security), or in the \"Config.xml\" file in the AppData directory.")]
        public ApiKey ApiKey { get; set; }

        /// <summary>
        /// <para type="description">Indicates that all API requests should not append the '/api' uri base.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter NoApiPrefix
        {
            get => _noApiPrefix;
            set => _noApiPrefix = value;
        }

        /// <summary>
        /// <para type="description">Passes through the connection testing the "/system/status" endpoint.</para>
        /// </summary>
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

            Context.SonarrUrl = null;
        }

        protected override void ProcessRecord()
        {

            Context.SonarrUrl = this.ParameterSetName == "ByServerName"
                ? new SonarrUrl(this.SonarrServerName, this.PortNumber, _useSsl, this.ReverseProxyUriBase, !_noApiPrefix)
                : new SonarrUrl(this.SonarrUrl, !_noApiPrefix);

            HttpClientHandler handler = null;
            this.CheckCertificateValidity(ref handler);

            Context.ApiCaller = this.MyInvocation.BoundParameters.ContainsKey("Proxy")
                ? NewApiCaller(Context.SonarrUrl, this.ApiKey, _allowRedirect, this.Proxy, this.ProxyCredential, _proxyBypass)
                : NewApiCaller(Context.SonarrUrl, this.ApiKey, _allowRedirect);

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

        public ApiCaller NewApiCaller(ISonarrUrl url, ApiKey apiKey, bool allowRedirects)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = allowRedirects,
                UseDefaultCredentials = true
            };
            return NewApiCaller(url.BaseUrl, apiKey, handler);
        }

        [Obsolete]
        public ApiCaller NewApiCaller(ApiKey apiKey, UriBuilder uriBuilder, bool allowRedirects)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = allowRedirects,
                UseDefaultCredentials = true
            };
            return this.NewApiCaller(uriBuilder.Uri.GetLeftPart(UriPartial.Scheme | UriPartial.Authority), apiKey, handler);
        }

        public ApiCaller NewApiCaller(ISonarrUrl url, ApiKey apiKey, bool allowRedirects, string proxy, ProxyCredential proxyCredential, bool proxyBypassLocal)
        {
            var wp = new WebProxy(proxy)
            {
                BypassProxyOnLocal = proxyBypassLocal,
            };

            if (proxyCredential != null)
                wp.Credentials = proxyCredential;

            else
                wp.UseDefaultCredentials = true;

            var proxyHandler = new HttpClientHandler
            {
                AllowAutoRedirect = allowRedirects,
                UseDefaultCredentials = true,
                Proxy = wp,
                UseProxy = true
            };
            return NewApiCaller(url.BaseUrl, apiKey, proxyHandler);
        }

        [Obsolete]
        public ApiCaller NewApiCaller(ApiKey apiKey, UriBuilder uriBuilder, bool allowRedirects, string proxy, ProxyCredential proxyCredential, bool proxyBypassLocal)
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

            return NewApiCaller(uriBuilder.Uri.GetLeftPart(UriPartial.Scheme | UriPartial.Authority), apiKey, handler);
        }

        private ApiCaller NewApiCaller(string url, ApiKey apiKey, HttpClientHandler handler)
        {
            this.CheckCertificateValidity(ref handler);

            return new ApiCaller(handler, apiKey)
            {
                BaseAddress = new Uri(url)
            };
        }

        #endregion
    }
}