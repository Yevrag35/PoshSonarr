using System;

namespace MG.Sonarr.Functionality.Url
{
    /// <summary>
    /// A filter parameter used in a <see cref="Uri"/> query path.
    /// </summary>
    public struct FilterLogParameter : IUrlParameter
    {
        private const string FORMAT = "filterKey={0}&filterValue={1}";
        private string _key;
        private string _value;

        public int Length => 23 + _key.Length + _value.Length;

        private FilterLogParameter(string sortKey, string value)
        {
            _key = sortKey;
            _value = value;
        }

        public string AsString() => string.Format(FORMAT, _key, _value);

        /// <summary>
        /// Creates a new instance of <see cref="FilterLogParameter"/> with the given key and value.
        /// </summary>
        /// <param name="key">The 'filterKey' used in the query parameter.</param>
        /// <param name="value">The 'filterValue' used in the query parameter.</param>
        public static FilterLogParameter Create(LogSortKey key, IConvertible value)
        {
            return new FilterLogParameter(key.ToString().ToLower(), Convert.ToString(value));
        }
    }
}
