using Sonarr.Api.Endpoints;
using Sonarr.Api.Results;
using System;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SonarrStatus", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(StatusResult))]
    public class GetSonarrStatus : BaseCmdlet
    {
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            var stat = new SystemStatus();
            result = Api.Send(stat);
            PipeBack<StatusResult>(result);
        }
    }
}
