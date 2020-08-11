using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality
{
    public interface IEpisode : IRecord
    {
        int? AbsoluteEpisodeNumber { get; }
        DateTime? AirDate { get; }
        int? EpisodeNumber { get; }
        bool IsMonitored { get; }
        string Name { get; }
        int? SeasonNumber { get; }
        long? SeriesId { get; }
    }
}
