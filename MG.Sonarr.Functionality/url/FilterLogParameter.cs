using System;

namespace MG.Sonarr.Functionality.Url
{
    /// <summary>
    /// A filter parameter used in a <see cref="Uri"/> query path.
    /// </summary>
    public struct FilterLogParameter : IUrlParameter
    {
        private string _key;
        private string _value;

        /// <summary>
        /// The 'filterKey' of the query parameter.
        /// </summary>
        public string Key
        {
            get => _key;
            set => _key = value.ToLower();
        }
        //IConvertible IUrlParameter.Key => this.Key;

        public int Length => 23 + _key.Length + _value.Length;
        /// <summary>
        /// The 'filterValue' of the query parameter.
        /// </summary>
        //public IConvertible Value
        //{
        //    get => _value;
        //    set => _value = Convert.ToString(value);
        //}

        /// <summary>
        /// Initializes a new instance of <see cref="FilterLogParameter"/> with the given key and value.
        /// </summary>
        /// <param name="key">The 'filterKey' used in the query parameter.</param>
        /// <param name="value">The 'filterValue' used in the query parameter.</param>
        public FilterLogParameter(string key, IConvertible value)
        {
            _key = key.ToLower();
            _value = Convert.ToString(value);
        }

        public string AsString()
        {
            return string.Format("filterKey={0}&filterValue={1}", _key, _value);
        }
    }
}
