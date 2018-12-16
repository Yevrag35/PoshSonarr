using Sonarr.Api.Cmdlets.Base;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Results;
using System;
using System.Linq;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SonarrCommandResult", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(FinalCommandResult))]
    public class GetCommandResult : BaseCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [Alias("Id")]
        public long JobId { get; set; }

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            var cmd = new Command(JobId);
            var result = Api.SonarrGetAs<FinalCommandResult>(cmd).ToArray()[0];
            WriteObject(result);
        }
    }
}
