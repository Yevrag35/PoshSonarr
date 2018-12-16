using Sonarr.Api.Endpoints;
using Sonarr.Api.Results;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets.Base
{
    [CmdletBinding(PositionalBinding = false)]
    public abstract class PipeableWithEpisode : GetSonarrEpisode
    {
        internal const string EPISODE_IDS = "episodeIds";

        [Parameter(Mandatory = true, ParameterSetName = "ByEpisodePipeline", ValueFromPipeline = true)]
        public EpisodeResult Episode { get; set; }

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (!MyInvocation.BoundParameters.ContainsKey("Episode"))
                base.ProcessRecord();
            else
            {
                IsEpisodeId = true;
                ids.Add(Episode.Id);
            }
        }

        protected override void EndProcessing()
        {
            if (!IsEpisodeId)
            {
                for (int i = ids.Count - 1; i >= 0; i--)
                {
                    var id = ids[i];
                    var cmd = new Episode(id, IsEpisodeId);
                    var ep = Api.SonarrGetAs<EpisodeResult>(cmd);
                    foreach (EpisodeResult er in ep)
                    {
                        ids.Add(er.Id);
                    }
                    ids.Remove(id);
                }
            }
        }
    }
}
