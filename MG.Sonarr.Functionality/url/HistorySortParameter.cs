using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality.Url
{
    public class HistorySortParameter : IUrlParameter
    {
        IConvertible IUrlParameter.Key => this.Key;
        public HistorySortKey Key { get; set; }

        IConvertible IUrlParameter.Value => this.Value;
        public SortDirection Value { get; set; } = SortDirection.Descending;

        public HistorySortParameter(HistorySortKey sortKey, SortDirection direction)
        {
            this.Key = sortKey;
            this.Value = direction;
        }

        public string AsString()
        {
            return string.Format("sortKey={0}&sortDir={1}", this.GetKeyEnumAsString(), this.Value.ToString().ToLower());
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
