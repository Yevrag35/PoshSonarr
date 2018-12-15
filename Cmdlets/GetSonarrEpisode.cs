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
    [OutputType(typeof(Sonarr.Api.Results.Episode))]
    public class GetSonarrEpisode : GetSonarrSeries
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ViaPipelineInput")]
        public SeriesResult Series { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByEpisodeId")]
        public int EpisodeId { get; set; }

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            var results = GetEpisode();
            foreach (var result in results)
            {
                PipeBack<Results.Episode>(result);
            }
        }

        private protected ApiResult[] GetEpisode()
        {
            var list = new List<ApiResult>();
            Endpoints.Episode endpoint = null;
            if (MyInvocation.BoundParameters.ContainsKey("Series"))
            {
                endpoint = new Endpoints.Episode(this.Series.Id, false);
                list.Add(GetEpisodeResult(endpoint));
            }
            else if (MyInvocation.BoundParameters.ContainsKey("SeriesId"))
            {
                endpoint = new Endpoints.Episode(SeriesId.Value, false);
                list.Add(GetEpisodeResult(endpoint));
            }
            else if (MyInvocation.BoundParameters.ContainsKey("EpisodeId"))
            {
                endpoint = new Endpoints.Episode(EpisodeId, true);
                list.Add(GetEpisodeResult(endpoint));
            }
            else if (MyInvocation.BoundParameters.ContainsKey("SeriesName"))
            {
                if (SonarrServiceContext.SonarrSeries == null)
                    SonarrServiceContext.SonarrSeries = base.GetAllSeries(new Series(null));

                var results = SearchFor(SeriesName);
                foreach (var result in results)
                {
                    list.Add(GetEpisodeResult(new Endpoints.Episode(result.Id, false)));
                }
            }
            return list.ToArray();
        }

        private protected ApiResult GetEpisodeResult(Endpoints.Episode endpoint) => Api.Send(endpoint);
    }
}
