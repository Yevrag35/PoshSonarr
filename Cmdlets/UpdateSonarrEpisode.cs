using MG.Api;
using Sonarr.Api.Results;
using Sonarr.Api.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
	[Cmdlet(VerbsData.Update, "SonarrEpisode", ConfirmImpact = ConfirmImpact.None,
		DefaultParameterSetName = "ViaEpisodePipelineInput")]
	[CmdletBinding(PositionalBinding = false)]
    public sealed class UpdateSonarrEpisode : GetSonarrEpisode
    {
		[Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ParameterSetName = "ViaEpisodePipelineInput")]
		public Episode Episode { get; set; }

        private List<Episode> eps;

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            eps = new List<Episode>();
        }

        protected override void ProcessRecord()
        {
            if (ParameterSetName != "ViaEpisodePipelineInput")
            {
                var results = GetEpisode();
                var eArr = ResultWithNoOutput<Episode>(results);
                eps.AddRange(eArr);
            }
            else
                eps.Add(Episode);
        }

        protected override void EndProcessing()
        {
            foreach (var ep in eps)
            {
                PipeBack<Episode>(SendUpdatedEpisodes(ep.ToJson()));
            }
        }

		private ApiResult SendUpdatedEpisodes(string jsonBody) =>
			Api.Send(new Endpoints.Episode(), jsonBody, SonarrMethod.PUT);
    }
}
