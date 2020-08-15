using System;

namespace MG.Sonarr.Functionality.Url
{
    public class HistorySortParameter : SortParameter, IUrlParameter
    {
        public HistorySortParameter(HistorySortKey sortKey, SortDirection direction)
            : base(direction)
        {
            base.AddSortKey(this.GetKeyEnumAsString(sortKey));
        }

        private string GetKeyEnumAsString(HistorySortKey key)
        {
            switch (key)
            {
                case HistorySortKey.SeriesTitle:
                    return "series.title";

                default:
                    return key.ToString().ToLower();
            }
        }
    }
}
