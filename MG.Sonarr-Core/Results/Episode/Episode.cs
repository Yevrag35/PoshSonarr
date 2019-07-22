using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Results
{
    public class EpisodeResult : BaseEpisodeResult
    {
        public EpisodeFile EpisodeFile { get; set; }
        public SeriesResult Series { get; set; }
    }
}
