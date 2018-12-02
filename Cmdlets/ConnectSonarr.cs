using MG.Api;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Results;
using System;
using System.Management.Automation;
using System.Net;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsCommunications.Connect, "Sonarr", DefaultParameterSetName = "ByUrl",
        ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(StatusResult))]
    public class ConnectSonarr : BaseCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByUrl")]
        public ApiUrl Url { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public ApiKey ApiKey { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByServer")]
        public string ServerName { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByServer")]
        public int Port { get; set; }

        private bool _ssl;
        [Parameter(Mandatory = false, ParameterSetName = "ByServer")]
        public SwitchParameter UseSSL
        {
            get => _ssl;
            set => _ssl = value;
        }

        private bool _nopre;
        [Parameter(Mandatory = false)]
        public SwitchParameter NoApiPrefix
        {
            get => _nopre;
            set => _nopre = value;
        }

        protected override void BeginProcessing()
        {
            switch (ParameterSetName)
            {
                case "ByUrl":
                    var checkUrl = (Uri)Url;
                    if (checkUrl.PathAndQuery != "/")
                        Url = checkUrl.ToString().Replace(checkUrl.PathAndQuery, string.Empty);

                    break;
                case "ByServer":
                    string prot = "http";
                    if (_ssl)
                        prot = "https";

                    string full = prot + "://" + ServerName + ":" + Port;
                    Url = full;
                    break;
            }
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            SonarrServiceContext.Value = Url;
            SonarrServiceContext.ApiKey = ApiKey;
            SonarrServiceContext.NoApiPrefix = _nopre;

            Api = new ApiCaller(SonarrServiceContext.Value);

            var stat = new SystemStatus();
            try
            {
                result = Api.Send(stat);
                PipeBack<StatusResult>(result);
            }
            catch (WebException e)
            {
                SonarrServiceContext.Clear();
                throw new WebException(e.Message, e, e.Status, e.Response);
            }
        }
    }
}
