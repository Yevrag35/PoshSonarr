using Sonarr.Api.Cmdlets.Base;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Enums;
using Sonarr.Api.Results;
using System;
using System.Linq;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Start, "SonarrRssSync", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    public class StartSonarrRssSync : NonPipeableCommandCmdlet
    {
        internal override SonarrCommand Command => SonarrCommand.RssSync;

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            var result = ProcessCommand(null);
            WriteObject(result);
        }
    }
}
