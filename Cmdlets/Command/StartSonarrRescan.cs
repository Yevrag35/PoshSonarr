using MG.Api;
using Sonarr.Api.Cmdlets.Base;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Enums;
using Sonarr.Api.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Start, "SonarrRescan", SupportsShouldProcess = true,
        DefaultParameterSetName = "ByPipeline")]
    [CmdletBinding(PositionalBinding = false)]
    public class StartSonarrRescan : PipeableWithSeriesCommand
    {
        internal override SonarrCommand Command => SonarrCommand.RescanSeries;

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord() => base.ProcessRecord();

        protected override void EndProcessing()
        {
            for (int i = 0; i < _list.Count; i++)
            {
                var ser = _list[i];
                var parameters = new Dictionary<string, object>()
                {
                    { SERIES_ID, ser.Id }
                };
                if (Force || ShouldContinue("Perform Rescan on series with ID " + Convert.ToString(ser.Id), "Are you sure?"))
                {
                    var result = ProcessCommand(parameters);
                    WriteObject(result);
                }
            }
        }
    }
}
