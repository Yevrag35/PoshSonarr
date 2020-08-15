using System;

namespace MG.Sonarr.Functionality.Url
{
    /// <summary>
    /// A URL query parameter for sorting log entries.
    /// </summary>
    public sealed class LogSortParameter : SortParameter, IUrlParameter
    {
        /// <summary>
        /// Initializes a new <see cref="LogSortParameter"/> indicating to sort the logs by the specified
        /// key in the specified direction.
        /// </summary>
        /// <param name="sortKey">The key to sort on.</param>
        /// <param name="direction">The direction of sorting.</param>
        public LogSortParameter(LogSortKey sortKey, SortDirection direction)
            : base(direction)
        {
            base.AddSortKey(sortKey.ToString().ToLower());
        }
    }
}
