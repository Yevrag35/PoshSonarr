using MG.Attributes;
using System;

namespace Sonarr.Api.Enums
{
    public enum SortDirection : int
    {
        [MGName("asc")]
        Ascending = 0,

        [MGName("desc")]
        Descending = 1
    }
}
