using System;

namespace MG.Sonarr.Functionality.Url
{
    /// <summary>
    /// A URL query parameter for sorting log entries.
    /// </summary>
    public class LogSortParameter : SortParameter, IUrlParameter
    {
        IConvertible IUrlParameter.Key => this.Key;
        /// <summary>
        /// The log property key that is sorted.
        /// </summary>
        public LogSortKey Key { get; set; }

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
