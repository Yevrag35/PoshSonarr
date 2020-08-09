using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    public interface IRecordPage
    {
        int Page { get; }
        int PageSize { get; }
        IReadOnlyList<IRecord> Records { get; }
        SortDirection SortDirection { get; }
        string SortKey { get; }
        int TotalRecords { get; }
    }
}
