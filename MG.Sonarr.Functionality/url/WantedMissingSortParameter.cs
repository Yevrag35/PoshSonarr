using System;

namespace MG.Sonarr.Functionality.Url
{
    public class WantedMissingSortParameter : SortParameter, IUrlParameter
    {
        private string _key;

        //IConvertible IUrlParameter.Key => this.Key;
        public WantedMissingSortKey Key
        {
            get => (WantedMissingSortKey)Enum.Parse(typeof(WantedMissingSortKey), _key);
            set => _key = value.ToString();
        }
        public int Length => 17 + _key.Length + base.SortDirectionString.Length;
        //IConvertible IUrlParameter.Value => base.Value;

        public WantedMissingSortParameter(WantedMissingSortKey sortKey, SortDirection direction)
            : base(direction)
        {
            _key = this.GetKeyEnumAsString(sortKey);
        }

        public string AsString()
        {
            return string.Format("sortKey={0}&sortDir={1}", _key, base.SortDirectionString);
        }

        private string GetKeyEnumAsString(WantedMissingSortKey sortKey)
        {
            switch (sortKey)
            {
                case WantedMissingSortKey.SeriesTitle:
                    return "series.title";

                default:
                    return this.Key.ToString().ToLower();
            }
        }
    }
}
