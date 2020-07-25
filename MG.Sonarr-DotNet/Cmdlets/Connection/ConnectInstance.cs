using MG.Api.Json;
using MG.Api.Json.Extensions;
using MG.Api.Rest.Extensions;
using MG.Api.Rest.Generic;
using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
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
    [Cmdlet(VerbsCommunications.Connect, "Instance", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByServerName", HelpUri = "https://github.com/Yevrag35/PoshSonarr/wiki/Connect-SonarrInstance")]
    [Alias("Connect-")]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(Status))]
    public partial class ConnectInstance : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string CONNECT_EP = "/system/status";
        private const string SLASH_STR = "/";
        private static readonly char SLASH = char.Parse(SLASH_STR);
        private static readonly char[] SLASH_API = new char[4]
        {
            SLASH, char.Parse("a"), char.Parse("p"), char.Parse("i")
        };

        private bool _allowRedirect;
        private bool _noApiPrefix;
        private bool _noCache;
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
        [ValidateNotNull]
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
        /// <para type="description">Indicates that all API requests should not append '/api' to the end of the URL path.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter NoApiPrefix
        {
            get => _noApiPrefix;
            set => _noApiPrefix = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter NoCache
        {
            get => _noCache;
            set => _noCache = value;
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
            this.SetSonarrUrl();

            HttpClientHandler handler = new HttpClientHandler();
            if (_skipCert)
                Validation.CheckCertificateValidity(ref handler);

            ISonarrClient client = ClassFactory.GenerateClient(handler, Context.SonarrUrl, this.ApiKey, _allowRedirect, this.Proxy, this.ProxyCredential, _proxyBypass);

            //if (this.ContainsParameter(x => x.Proxy))
            //    this.BoundCallerWithProxy(handler);
            
            //else
            //    this.BoundCallerWithoutProxy(handler);

            Status statusResult = this.TryConnect(client);

            if (statusResult != null)
            {
                this.InitializeContext(client);

                if (_passThru)
                    base.WriteObject(statusResult);
            }
        }

        #endregion

        #region PRIVATE/BACKEND METHODS
        //private void BoundCallerWithProxy(HttpClientHandler handler)
        //{
        //    handler.Proxy = new WebProxy(this.Proxy, _proxyBypass, null, this.ProxyCredential);
        //    handler.AllowAutoRedirect = _allowRedirect;
        //    Context.ApiCaller = new SonarrRestClient(handler)
        //    {
        //        BaseAddress = Context.SonarrUrl.Url
        //    };
        //    Context.ApiCaller.AddApiKey(this.ApiKey);
        //}
        //private void BoundCallerWithoutProxy(HttpClientHandler handler)
        //{
        //    handler.AllowAutoRedirect = _allowRedirect;
        //    Context.ApiCaller = new SonarrRestClient(handler)
        //    {
        //        BaseAddress = Context.SonarrUrl.Url
        //    };
        //    Context.ApiCaller.AddApiKey(this.ApiKey);
        //}

        private void InitializeContext(ISonarrClient client)
        {
            Context.Initialize(client, !_noApiPrefix, !_noCache);
            //History.Initialize();
            //List<QualityDefinition> definitions = base.SendSonarrListGet<QualityDefinition>("/qualitydefinition");
            //Context.AllQualities = new QualityDictionary(definitions.Select(x => x.Quality));
            //Context.TagManager = ClassFactory.NewTagManager(Context.ApiCaller, !_noApiPrefix);

            //List<IndexerSchema> schemas = base.SendSonarrListGet<IndexerSchema>(ApiEndpoint.IndexerSchema);
            //Context.IndexerSchemas = IndexerSchemaCollection.FromSchemas(schemas);
            //Context.NoCache = _noCache;
        }

        private void SetSonarrUrl() => Context.SonarrUrl = !this.ContainsParameter(x => x.SonarrUrl)
                ? ClassFactory.GenerateSonarrUrl(this.SonarrServerName, this.PortNumber, _useSsl, this.ReverseProxyUriBase, !_noApiPrefix)
                : ClassFactory.GenerateSonarrUrl(this.SonarrUrl, !_noApiPrefix);
        private Status TryConnect(ISonarrClient client)
        {
            base.WriteApiDebug(CONNECT_EP, HttpMethod.Get, out string apiPath);
            IRestResponse<Status> response = client.GetAsJsonAsync<Status>(apiPath).GetAwaiter().GetResult();
            if (response.IsFaulted)
            {
                if (response.HasException)
                    throw response.GetAbsoluteException();

                else if (!response.IsValidStatusCode)
                    throw new NoSonarrResponseException();

                else
                    throw new HttpStatusException(response.StatusCode);
            }   
            else
            {
                return response.Content;
            }
        }

        #endregion
    }
}