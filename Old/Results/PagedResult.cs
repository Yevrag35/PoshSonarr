using System;

namespace Sonarr.Api.Results
{
    public abstract class PagedResult : SonarrResult
    {
        public int Page { get; internal set; }
        public int PageSize { get; internal set; }
        public string SortKey { get; internal set; }
        public string SortDirection { get; internal set; }
        public int TotalRecords { get; internal set; }

        public PagedResult() : base() { }
    }
}
