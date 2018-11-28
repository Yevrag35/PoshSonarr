using Sonarr.Api.Endpoints;
using Sonarr.Api.Results;
using System;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SonarrEpisode", ConfirmImpact = ConfirmImpact.None,
        DefaultParameterSetName = "BySeriesName")]
    [OutputType(typeof(Episode))]
    public class GetSonarrEpisode : BaseCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesName")]
        public string SeriesName { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ViaPipelineInput")]
        public SeriesResult Series { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByEpisodeId")]
        public int EpisodeId { get; set; }

        protected override void BeginProcessing() => base.BeginProcessing();


    }
}
