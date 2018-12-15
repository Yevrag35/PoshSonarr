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
    [Cmdlet(VerbsCommon.Rename, "SonarrSeriesFiles")]
    public class RenameSeriesFiles : PipeableWithSeriesCommand
    {
        private const string SERIES_IDS = "seriesIds";

        internal override SonarrCommand Command => SonarrCommand.RenameSeries;

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord() => base.ProcessRecord();

        protected override void EndProcessing()
        {
            var ids = new List<int>(_list.Count);
            for (int i = 0; i < _list.Count; i++)
            {
                ids.Add(Convert.ToInt32(_list[i].Id));
            }
            var parameters = new Dictionary<string, object>()
            {
                { SERIES_IDS, ids }
            };
            if (Force || ShouldContinue("Perform Series Rename on the following ids: " + string.Join(", ", ids), "Are you sure?"))
            {
                var result = ProcessCommand(parameters);
                WriteObject(result);
            }
        }
    }
}
