using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Cmdlets.Connection
{
    [Cmdlet(VerbsCommunications.Connect, "Instance", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByServerName")]
    [CmdletBinding(PositionalBinding = false)]
    public class ConnectInstance : PSCmdlet
    {
        #region FIELDS/CONSTANTS
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

        [Parameter(Mandatory = true)]
        [Alias("key")]
        public ApiKey ApiKey { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter NoApiPrefix { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() { }

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

            var apiCaller = new ApiCaller(url, this.ApiKey);
            Context.ApiCaller = apiCaller;
            if (this.NoApiPrefix.ToBool())
            {
                Context.NoApiPrefix = true;
            }

            if (this.PassThru.ToBool())
            {
                string status = Context.ApiCaller.SonarrGet("/system/status");
                SonarrStatusResult sr = SonarrHttpClient.ConvertToSonarrResult<SonarrStatusResult>(status);
                sr.UrlBase = Context.ApiCaller.BaseAddress;
                base.WriteObject(sr);
            }
        }

        #endregion
    }
}