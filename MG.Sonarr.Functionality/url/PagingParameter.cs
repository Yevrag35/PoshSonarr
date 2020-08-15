using System;

namespace MG.Sonarr.Functionality.Url
{
    /// <summary>
    /// A URL query parameter for paging log results.
    /// </summary>
    public struct PagingParameter : IEquatable<IUrlParameter>, IEquatable<PagingParameter>, IUrlParameter
    {
        private const string FORMAT = "page={0}&pageSize={1}";
        public const int DefaultPage = 1;
        public const int DefaultPageSize = 10;
        private string _key;
        private string _val;

        public int Length => 15 + _key.Length + _val.Length;

        private PagingParameter(string pageNumber, string pageSize)
        {
            _key = pageNumber;
            _val = pageSize;
        }

        public string AsString()
        {
            return string.Format(FORMAT, _key, _val);
        }

        /// <summary>
        /// Initializes a new <see cref="PagingParameter"/> with the specified page number and page size.
        /// </summary>
        /// <param name="pageNumber">The page number to start the query at.</param>
        /// <param name="pageSize">The number of results returned on 1 page.</param>
        public static PagingParameter Create(int pageNumber = DefaultPage, int pageSize = DefaultPageSize)
        {
            return new PagingParameter(pageNumber.ToString(), pageSize.ToString());
        }

        public bool Equals(IUrlParameter other)
        {
            if (other is PagingParameter pp)
                return _key.Equals(pp._key) && _val.Equals(pp._val);

            else
                return false;
        }
        public bool Equals(PagingParameter other)
        {
            return this.Length.Equals(other.Length) &&
                _key.Equals(other._key)
                &&
                _val.Equals(other._val);
        }
    }
}
