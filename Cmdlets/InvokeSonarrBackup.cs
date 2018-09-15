using MG.Api;
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
    public class BackupSonarr : BaseCommandCmdlet
    {
        public override SonarrCommand Command => SonarrCommand.Backup;
        public override SonarrMethod Method => SonarrMethod.POST;

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            if (!SonarrServiceContext.IsSet)
                throw new SonarrContextNotSetException("  Run the 'Connect-Sonarr' cmdlet first.");

            Api = new ApiCaller(SonarrServiceContext.Value);
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            result = ApplyCommandToAll();
            PipeBackResult(result);
        }
    }
}
