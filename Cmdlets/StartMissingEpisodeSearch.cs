using Newtonsoft.Json;
using Sonarr.Api.Cmdlets.Base;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Enums;
using Sonarr.Api.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Start, "SonarrMissingEpisodeSearch", ConfirmImpact = ConfirmImpact.None, SupportsShouldProcess = true)]
    [CmdletBinding(PositionalBinding = false)]
    public class StartMissingEpisodeSearch : NonPipeableCommandCmdlet
    {
        internal override SonarrCommand Command => SonarrCommand.missingEpisodeSearch;

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            var result = ProcessCommand(null);
            WriteObject(result);
        }
    }
}
