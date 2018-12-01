using MG.Attributes;
using System;

namespace Sonarr.Api.Enums
{
    public enum SortKey : int
    {
        [MGName("series.title")]
        SeriesTitle = 0,

        [MGName("airDateUtc")]
        AirDate = 1
    }
}
