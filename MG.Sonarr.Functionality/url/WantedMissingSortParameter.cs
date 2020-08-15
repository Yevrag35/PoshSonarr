using System;

namespace MG.Sonarr.Functionality.Url
{
    public sealed class WantedMissingSortParameter : SortParameter, IUrlParameter
    {
        public WantedMissingSortParameter(WantedMissingSortKey sortKey, SortDirection direction)
            : base(direction)
        {
            base.AddSortKey(this.GetKeyEnumAsString(sortKey));
        }

        private string GetKeyEnumAsString(WantedMissingSortKey sortKey)
        {
            switch (sortKey)
            {
                case WantedMissingSortKey.SeriesTitle:
                    return "series.title";

                default:
                    return sortKey.ToString().ToLower();
            }
        }
    }
}
