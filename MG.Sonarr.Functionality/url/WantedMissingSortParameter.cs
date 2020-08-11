using System;

namespace MG.Sonarr.Functionality.Url
{
    public class WantedMissingSortParameter : SortParameter, IUrlParameter
    {
        IConvertible IUrlParameter.Key => this.Key;
        public WantedMissingSortKey Key { get; set; }

        IConvertible IUrlParameter.Value => base.Value;

        public WantedMissingSortParameter(WantedMissingSortKey sortKey, SortDirection direction)
            : base(direction)
        {
            this.Key = sortKey;
        }

        public string AsString()
        {
            return string.Format("sortKey={0}&sortDir={1}", this.GetKeyEnumAsString(), base.GetSortString());
        }

        private string GetKeyEnumAsString()
        {
            switch (this.Key)
            {
                case WantedMissingSortKey.SeriesTitle:
                    return "series.title";

                default:
                    return this.Key.ToString().ToLower();
            }
        }
    }
}
