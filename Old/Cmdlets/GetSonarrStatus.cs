using Sonarr.Api.Cmdlets.Base;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Results;
using System;
using System.Linq;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SonarrStatus", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(StatusResult))]
    public class GetSonarrStatus : BaseCmdlet
    {
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            var stat = new SystemStatus();
            var result = Api.SonarrGetAs<StatusResult>(stat).ToArray()[0];
            StatusResult final = GetRealTime(result);

            WriteObject(final);
        }

        internal static StatusResult GetRealTime(StatusResult sr)
        {
            if (sr.BuildTime.HasValue)
            {
                var realTime = sr.BuildTime.Value.ToLocalTime();
                sr.BuildTimeUtc = sr.BuildTime;
                sr.BuildTime = realTime;
            }
            return sr;
        }
    }
}
