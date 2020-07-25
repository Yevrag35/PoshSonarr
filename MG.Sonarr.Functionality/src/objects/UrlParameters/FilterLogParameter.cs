using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality
{
    /// <summary>
    /// A filter parameter used in a <see cref="Uri"/> query path.
    /// </summary>
    public class FilterLogParameter : IUrlParameter
    {
        /// <summary>
        /// The 'filterKey' of the query parameter.
        /// </summary>
        public IConvertible Key { get; set; }

        /// <summary>
        /// The 'filterValue' of the query parameter.
        /// </summary>
        public IConvertible Value { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="FilterLogParameter"/> with the given key and value.
        /// </summary>
        /// <param name="key">The 'filterKey' used in the query parameter.</param>
        /// <param name="value">The 'filterValue' used in the query parameter.</param>
        public FilterLogParameter(IConvertible key, IConvertible value)
        {
            this.Key = key;
            this.Value = value;
        }

        public string AsString()
        {
            return string.Format("filterKey={0}&filterValue={1}", Convert.ToString(this.Key).ToLower(), Convert.ToString(this.Value));
        }
    }
}
