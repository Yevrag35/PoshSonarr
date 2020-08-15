using System;

namespace MG.Sonarr.Functionality.Url
{
    public class HistorySortParameter : SortParameter, IUrlParameter
    {
        private string _sortKey;

        //IConvertible IUrlParameter.Key => this.Key;
        public HistorySortKey Key
        {
            get => (HistorySortKey)Enum.Parse(typeof(HistorySortKey), _sortKey);
            set => _sortKey = value.ToString();
        }
        public int Length => 17 + _sortKey.Length + base.SortDirectionString.Length;
        //IConvertible IUrlParameter.Value => base.SortDirectionString;

        public HistorySortParameter(HistorySortKey sortKey, SortDirection direction)
            : base(direction)
        {
            this.Key = sortKey;
        }

        public string AsString()
        {
            return string.Format("sortKey={0}&sortDir={1}", this.GetKeyEnumAsString(), base.SortDirectionString);
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
