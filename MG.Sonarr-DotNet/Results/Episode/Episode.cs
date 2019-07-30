using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class defining a response from the "/episode" endpoint.
    /// </summary>
    [Serializable]
    public class EpisodeResult : BaseEpisodeResult
    {
        public EpisodeFile EpisodeFile { get; set; }
        public int SeasonNumber { get; set; }
        public SeriesResult Series { get; set; }
    }
}
