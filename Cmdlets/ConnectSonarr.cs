using MG.Api;
using System;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsCommunications.Connect, "Sonarr")]
    [OutputType(typeof(void))]
    public class ConnectSonarr : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public ApiUrl Url { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public ApiKey ApiKey { get; set; }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            SonarrServiceContext.Value = Url;
            SonarrServiceContext.ApiKey = ApiKey;
        }
    }
}
