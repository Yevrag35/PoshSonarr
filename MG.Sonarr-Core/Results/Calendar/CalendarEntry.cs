using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    public class CalendarEntry : BaseEpisodeResult
    {
        public int SceneAbsoluteEpisodeNumber { get; set; }
        public int SceneEpisodeNumber { get; set; }
        public int SceneSeasonNumber { get; set; }
    }
}
