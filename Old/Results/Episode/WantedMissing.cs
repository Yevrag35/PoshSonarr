using System;

namespace Sonarr.Api.Results
{
    public class WantedMissingResult : PagedResult
    {
        internal override string[] SkipThese => null;

        public SonarrArray<EpisodeResult> Records { get; internal set; }

        public WantedMissingResult() { }
    }
}