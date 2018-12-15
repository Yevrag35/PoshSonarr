using MG.Api;
using Sonarr.Api.Cmdlets.Base;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Enums;
using Sonarr.Api.Results;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Start, "SonarrRefresh", SupportsShouldProcess = true,
        DefaultParameterSetName = "ByPipeline")]
    [CmdletBinding(PositionalBinding = false)]
    public class StartSonarrRefresh : PipeableWithSeriesCommand
    {
        internal override SonarrCommand Command => SonarrCommand.RefreshSeries;

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
                if (Force || ShouldContinue("Perform Refresh on series with ID " + Convert.ToString(ser.Id), "Are you sure?"))
                {
                    var result = ProcessCommand(parameters);
                    WriteObject(result);
                }
            }
        }

        //protected override void ProcessRecord()
        //{
        //    base.ProcessRecord();
        //    switch (ParameterSetName)
        //    {
        //        case "ByPipeline":
        //            if (Series == null)
        //            {
        //                if (ShouldContinue("All Sonarr Series", "Refresh all series?  This may produce a lot of network activity.") ||
        //                    _force)
        //                {
        //                    result = ApplyCommandToAll();
        //                }
        //                else
        //                {
        //                    return;
        //                }
        //            }
        //            else
        //            {
        //                result = ApplyCommandToOne(new object[2] { "seriesId", Series.Id });
        //            }
        //            break;
        //        case "BySeriesId":
        //            result = ApplyCommandToOne(new object[2] { "seriesId", SeriesId });
        //            break;
        //    }
        //    PipeBackResult(result);
        //}
    }
}
