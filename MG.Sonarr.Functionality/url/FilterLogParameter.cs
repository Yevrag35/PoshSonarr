using System;

namespace MG.Sonarr.Functionality.Url
{
    /// <summary>
    /// A filter parameter used in a <see cref="Uri"/> query path.
    /// </summary>
    public struct FilterLogParameter : IUrlParameter
    {
        private string _key;
        private IConvertible _value;

        /// <summary>
        /// The 'filterKey' of the query parameter.
        /// </summary>
        public string Key
        {
            get => _key;
            set => _key = value;
        }
        IConvertible IUrlParameter.Key => this.Key;

        /// <summary>
        /// The 'filterValue' of the query parameter.
        /// </summary>
        public IConvertible Value
        {
            get => _value;
            set => _value = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FilterLogParameter"/> with the given key and value.
        /// </summary>
        /// <param name="key">The 'filterKey' used in the query parameter.</param>
        /// <param name="value">The 'filterValue' used in the query parameter.</param>
        public FilterLogParameter(string key, IConvertible value)
        {
            _key = key;
            _value = value;
        }

        public string AsString()
        {
            return string.Format("filterKey={0}&filterValue={1}", Convert.ToString(this.Key).ToLower(), Convert.ToString(this.Value));
        }
    }
}
