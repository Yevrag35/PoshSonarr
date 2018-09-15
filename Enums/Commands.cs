using System;

namespace Sonarr.Api.Enums
{
    public enum SonarrCommand : int
    {
        RefreshSeries = 0,
        RescanSeries = 1,
        EpisodeSearch = 2,
        SeasonSearch = 3,
        SeriesSearch = 4,
        DownloadedEpisodesScan = 5,
        RssSync = 6,
        RenameFiles = 7,
        RenameSeries = 8,
        Backup = 9,
        missingEpisodeSearch = 10
    }
}
