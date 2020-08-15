using System;
using System.Net;

namespace MG.Sonarr.Functionality.Url
{
    /// <summary>
    /// A folder parameter used in a <see cref="Uri"/> query path.
    /// </summary>
    public struct FolderParameter : IUrlParameter
    {
        private const string _key = "folder";
        private string _value;

        public int Length => 1 + _key.Length + _value.Length;

        /// <summary>
        /// Initializes a new <see cref="FolderParameter"/> querying the specified folder path.
        /// </summary>
        /// <param name="path">The folder path used in the query.</param>
        public FolderParameter(string path) => _value = path;

        /// <summary>
        /// Returns the query parameter in its <see cref="Uri"/> query form.
        /// </summary>
        /// <remarks>
        ///     The folder path will always be URL-encoded in the  process.
        /// </remarks>
        /// <returns>A string, URL-encoded query parameter.</returns>
        public string AsString() => string.Format(UrlParameter.KEY_VALUE_FORMAT, _key, WebUtility.UrlEncode(_value));
        public bool Equals(IUrlParameter other)
        {
            if (other is FolderParameter fp)
                return _value.Equals(fp._value);

            else
                return false;
        }
    }
}
