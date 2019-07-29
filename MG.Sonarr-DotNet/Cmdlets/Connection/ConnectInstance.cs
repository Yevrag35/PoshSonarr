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

namespace MG.Sonarr.Cmdlets.Connection
{
    [Cmdlet(VerbsCommunications.Connect, "Instance", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByServerName")]
    [CmdletBinding(PositionalBinding = false)]
    [Alias("Connect-", "conson")]
    public class ConnectInstance : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/system/status";

        private const string URL_FORMAT = "{0}://{1}:{2}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByServerName")]
        [Alias("Server", "ServerName")]
        public string SonarrServerName = "localhost";

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySonarrUrl")]
        [Alias("Url")]
        public Uri SonarrUrl { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = "ByServerName")]
        [Alias("Port")]
        public int PortNumber = 8989;

        [Parameter(Mandatory = false, ParameterSetName = "ByServerName")]
        public SwitchParameter UseSSL { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter SkipCertificateCheck { get; set; }

        [Parameter(Mandatory = false)]
        public string Proxy { get; set; }

        [Parameter(Mandatory = false)]
        public ProxyCredential ProxyCredential { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter ProxyBypassOnLocal { get; set; }

        [Parameter(Mandatory = true)]
        [Alias("key")]
        public ApiKey ApiKey { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter NoApiPrefix { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => Context.NoApiPrefix = false;

        protected override void ProcessRecord()
        {
            Uri url = null;
            if (this.ParameterSetName == "ByServerName")
            {
                string scheme = !this.UseSSL.ToBool()
                    ? "http"
                    : "https";

                url = new Uri(string.Format(URL_FORMAT, scheme, this.SonarrServerName, this.PortNumber), UriKind.Absolute);
            }
            else
            {
                url = this.SonarrUrl;
            }

            var handler = new HttpClientHandler()
            {
                UseDefaultCredentials = true
            };

            if (this.SkipCertificateCheck.ToBool())
            {
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            }
            if (this.MyInvocation.BoundParameters.ContainsKey("Proxy"))
            {
                var wp = new WebProxy(this.Proxy);
                if (this.MyInvocation.BoundParameters.ContainsKey("ProxyCredential"))
                {
                    wp.Credentials = this.ProxyCredential;
                }
                if (this.MyInvocation.BoundParameters.ContainsKey("ProxyBypassOnLocal"))
                {
                    wp.BypassProxyOnLocal = this.ProxyBypassOnLocal.ToBool();
                }
                handler.Proxy = wp;
                handler.UseProxy = true;
            }

            var apiCaller = new ApiCaller(handler, this.ApiKey)
            {
                //BaseAddress = url
                BaseAddress = new Uri(url.GetLeftPart(UriPartial.Scheme | UriPartial.Authority))
            };

            Context.ApiCaller = apiCaller;
            if (this.NoApiPrefix.ToBool())
            {
                Context.NoApiPrefix = true;
            }

            if (this.PassThru.ToBool())
            {
                string status = null;
                try
                {
                    status = Context.ApiCaller.SonarrGet(EP);
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message, e);
                }
                if (string.IsNullOrEmpty(status))
                {
                    throw new NoSonarrResponseException(apiCaller);
                }
                else
                {
                    SonarrStatusResult sr = null;
                    try
                    {
                        sr = SonarrHttpClient.ConvertToSonarrResult<SonarrStatusResult>(status);
                        sr.UrlBase = Context.ApiCaller.BaseAddress;
                        base.WriteObject(sr);
                    }
                    catch (Exception ex)
                    {
                        base.WriteError(ex, ErrorCategory.ParserError, status);
                    }
                }
            }
        }

        #endregion
    }
}