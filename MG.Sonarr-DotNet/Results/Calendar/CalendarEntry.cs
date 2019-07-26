using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    public class CalendarEntry : BaseEpisodeResult
    {
        public EpisodeFile EpisodeFile { get; set; }
        public int SceneAbsoluteEpisodeNumber { get; set; }
        public int SceneEpisodeNumber { get; set; }
        public int SceneSeasonNumber { get; set; }
        public SeriesResult Series { get; set; }
    }
}
