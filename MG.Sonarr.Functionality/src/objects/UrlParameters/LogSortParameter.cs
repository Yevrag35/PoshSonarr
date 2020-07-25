using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality
{
    /// <summary>
    /// A URL query parameter for sorting log entries.
    /// </summary>
    public class LogSortParameter : IUrlParameter
    {
        IConvertible IUrlParameter.Key => this.Key;
        /// <summary>
        /// The log property key that is sorted.
        /// </summary>
        public LogSortKey Key { get; set; }

        IConvertible IUrlParameter.Value => this.Value;
        /// <summary>
        /// The direction the log property key is sorted.
        /// </summary>
        public SortDirection Value { get; set; }

        /// <summary>
        /// Initializes a new <see cref="LogSortParameter"/> indicating to sort the logs by the specified
        /// key in the specified direction.
        /// </summary>
        /// <param name="sortKey">The key to sort on.</param>
        /// <param name="direction">The direction of sorting.</param>
        public LogSortParameter(LogSortKey sortKey, SortDirection direction)
        {
            this.Key = sortKey;
            this.Value = direction;
        }

        public string AsString()
        {
            string dir = "desc";
            if (this.Value == SortDirection.Ascending)
                dir = "asc";

            return string.Format("sortKey={0}&sortDir={1}", this.Key.ToString().ToLower(), dir);
        }
    }
}
