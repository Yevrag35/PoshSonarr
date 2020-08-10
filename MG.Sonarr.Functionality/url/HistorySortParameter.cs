using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality.Url
{
    public class HistorySortParameter : SortParameter, IUrlParameter
    {
        IConvertible IUrlParameter.Key => this.Key;
        public HistorySortKey Key { get; set; }

        IConvertible IUrlParameter.Value => base.Value;

        public HistorySortParameter(HistorySortKey sortKey, SortDirection direction)
            : base(direction)
        {
            this.Key = sortKey;
        }

        public string AsString()
        {
            return string.Format("sortKey={0}&sortDir={1}", this.GetKeyEnumAsString(), this.GetSortString());
        }

        private string GetKeyEnumAsString()
        {
            switch (this.Key)
            {
                case HistorySortKey.SeriesTitle:
                    return "series.title";

                default:
                    return this.Key.ToString().ToLower();
            }
        }
    }
}
