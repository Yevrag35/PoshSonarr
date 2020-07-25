using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality
{
    /// <summary>
    /// A URL query parameter for paging log results.
    /// </summary>
    public class PagingParameter : IUrlParameter
    {
        public const int DefaultPage = 1;
        public const int DefaultPageSize = 10;
        private int _key;
        private int _val;

        IConvertible IUrlParameter.Key => this.Key;
        /// <summary>
        /// The page number to start the query at.
        /// </summary>
        /// <remarks>
        ///     If the page number is set to 0 or lower, the default page number will be used (1).
        /// </remarks>
        public int Key
        {
            get => _key;
            set
            {
                if (value <= 0)
                    _key = DefaultPage;

                else
                    _key = value;
            }
        }
        IConvertible IUrlParameter.Value => this.Value;
        /// <summary>
        /// The number of results returned on 1 page.
        /// </summary>
        /// <remarks>
        ///     If the value is set to 0 or lower, the default page size will be used (10).
        /// </remarks>
        public int Value
        {
            get => _val;
            set
            {
                if (value <= 0)
                    _val = DefaultPageSize;

                else
                    _val = value;
            }
        }

        /// <summary>
        /// Initializes a new <see cref="PagingParameter"/> with the specified page number and page size.
        /// </summary>
        /// <param name="pageNumber">The page number to start the query at.</param>
        /// <param name="pageSize">The number of results returned on 1 page.</param>
        public PagingParameter(int pageNumber = DefaultPage, int pageSize = DefaultPageSize)
        {
            this.Key = pageNumber;
            this.Value = pageSize;
        }

        public string AsString()
        {
            return string.Format("page={0}&pageSize={1}", this.Key, this.Value);
        }
    }
}
