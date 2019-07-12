using MG.Attributes;
using System;

namespace Sonarr.Api.Enums
{
    public enum WantedMissingSortKey : int
    {
        [MGName("series.title")]
        SeriesTitle = 0,

        [MGName("airDateUtc")]
        AirDate = 1
    }

    public enum HistorySortKey : int
    {
        [MGName("series.title")]
        SeriesTitle = 0,

        [MGName("date")]
        Date = 1
    }
}
