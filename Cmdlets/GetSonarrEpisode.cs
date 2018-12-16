using MG.Api;
using Sonarr.Api.Cmdlets.Base;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SonarrEpisode", ConfirmImpact = ConfirmImpact.None,
        DefaultParameterSetName = "BySeriesTitle")]
    [OutputType(typeof(Sonarr.Api.Results.EpisodeResult))]
    public class GetSonarrEpisode : PipeableWithSeries
    {
        protected private List<long> ids;
        protected private bool IsEpisodeId = false;

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByEpisodeId")]
        public long[] EpisodeId { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            ids = new List<long>();
        }

        protected override void ProcessRecord()
        {
            if (!MyInvocation.BoundParameters.ContainsKey("Series") && !MyInvocation.BoundParameters.ContainsKey("EpisodeId"))
                base.ProcessRecord();

            else if (MyInvocation.BoundParameters.ContainsKey("Series"))
                _list.Add(Series);

            else
            {
                ids.AddRange(EpisodeId);
                IsEpisodeId = true;
            }

            for (int i = 0; i < _list.Count; i++)
            {
                ids.Add(_list[i].Id);
            }
        }

        protected override void EndProcessing()
        {
            for (int i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                var cmd = new Episode(id, IsEpisodeId);
                var ep = Api.SonarrGetAs<EpisodeResult>(cmd);
                WriteObject(ep, true);
            }
        }
    }
}
