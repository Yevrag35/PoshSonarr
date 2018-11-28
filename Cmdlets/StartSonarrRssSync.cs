using MG.Api;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Enums;
using Sonarr.Api.Results;
using System;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Start, "SonarrRssSync")]
    public class StartSonarrRssSync : BaseCommandCmdlet
    {
        public override SonarrCommand Command => SonarrCommand.RssSync;
        public override SonarrMethod Method => SonarrMethod.POST;

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            result = ApplyCommandToAll();

            PipeBackResult(result);
        }
    }
}
