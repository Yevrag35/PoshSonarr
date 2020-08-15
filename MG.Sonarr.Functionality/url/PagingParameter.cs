using System;

namespace MG.Sonarr.Functionality.Url
{
    /// <summary>
    /// A URL query parameter for paging log results.
    /// </summary>
    public struct PagingParameter : IUrlParameter
    {
        private const string DEFAULT_PAGE = "1";
        private const string DEFAULT_PAGE_SIZE = "10";
        public const int DefaultPage = 1;
        public const int DefaultPageSize = 10;
        private string _key;
        private string _val;

        //IConvertible IUrlParameter.Key => this.Key;
        /// <summary>
        /// The page number to start the query at.
        /// </summary>
        /// <remarks>
        ///     If the page number is set to 0 or lower, the default page number will be used (1).
        /// </remarks>
        public int Key
        {
            get => Convert.ToInt32(_key);
            set
            {
                if (value <= 0)
                    _key = DEFAULT_PAGE;

                else
                    _key = Convert.ToString(value);
            }
        }
        public int Length => 15 + _key.Length + _val.Length;
        //IConvertible IUrlParameter.Value => this.Value;
        /// <summary>
        /// The number of results returned on 1 page.
        /// </summary>
        /// <remarks>
        ///     If the value is set to 0 or lower, the default page size will be used (10).
        /// </remarks>
        public int Value
        {
            get => Convert.ToInt32(_val);
            set
            {
                if (value <= 0)
                    _val = DEFAULT_PAGE_SIZE;

                else
                    _val = Convert.ToString(value);
            }
        }

        /// <summary>
        /// Initializes a new <see cref="PagingParameter"/> with the specified page number and page size.
        /// </summary>
        /// <param name="pageNumber">The page number to start the query at.</param>
        /// <param name="pageSize">The number of results returned on 1 page.</param>
        public PagingParameter(int pageNumber = DefaultPage, int pageSize = DefaultPageSize)
        {
            _key = Convert.ToString(pageNumber);
            _val = Convert.ToString(pageSize);
        }

        public string AsString()
        {
            return string.Format("page={0}&pageSize={1}", this.Key, this.Value);
        }
    }
}
