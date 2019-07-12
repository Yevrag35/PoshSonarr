using Sonarr.Api.Cmdlets.Base;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Results;
using System;
using System.Linq;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SonarrDisk", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(DiskspaceResult))]
    public class GetDiskspace : BaseCmdlet
    {
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            var ds = new Diskspace();
            var result = Api.SonarrGetAs<DiskspaceResult>(ds);
            WriteObject(result, true);
        }
    }
}
