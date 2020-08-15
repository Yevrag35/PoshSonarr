using System;

namespace MG.Sonarr.Functionality.Url
{
    /// <summary>
    /// A URL query parameter for sorting log entries.
    /// </summary>
    public class LogSortParameter : SortParameter, IUrlParameter
    {
        private string _key;
        IConvertible IUrlParameter.Key => _key;
        /// <summary>
        /// The log property key that is sorted.
        /// </summary>
        public LogSortKey Key
        {
            get => (LogSortKey)Enum.Parse(typeof(LogSortKey), _key);
            set => _key = value.ToString();
        }
        public int Length => 17 + _key.Length + base.SortDirectionString.Length;
        IConvertible IUrlParameter.Value => this.Value;

        /// <summary>
        /// Initializes a new <see cref="LogSortParameter"/> indicating to sort the logs by the specified
        /// key in the specified direction.
        /// </summary>
        /// <param name="sortKey">The key to sort on.</param>
        /// <param name="direction">The direction of sorting.</param>
        public LogSortParameter(LogSortKey sortKey, SortDirection direction)
            : base(direction)
        {
            this.Key = sortKey;
        }

        public string AsString()
        {
            return string.Format("sortKey={0}&sortDir={1}", this.Key.ToString().ToLower(), SonarrFactory.GetSortDirectionValue(this.Value));
        }
    }
}
