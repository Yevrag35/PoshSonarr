using Sonarr.Api.Cmdlets.Base;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Enums;
using Sonarr.Api.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsData.Backup, "Sonarr")]
    [OutputType(typeof(CommandResult))]
    public class BackupSonarr : NonPipeableCommandCmdlet
    {
        internal override SonarrCommand Command => SonarrCommand.Backup;

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            var result = ProcessCommand(null);
            WriteObject(result);
        }
    }
}
