using System;
using System.Net;

namespace MG.Sonarr.Functionality.Url
{
    /// <summary>
    /// A folder parameter used in a <see cref="Uri"/> query path.
    /// </summary>
    public class FolderParameter : IUrlParameter
    {
        IConvertible IUrlParameter.Key => this.Key;
        /// <summary>
        /// The key of the query parameter which is statically set to 'folder'.
        /// </summary>
        public string Key => "folder";

        IConvertible IUrlParameter.Value => this.Value;
        /// <summary>
        /// The value of the query parameter indicating the folder path to query.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Initializes a new <see cref="FolderParameter"/> querying the specified folder path.
        /// </summary>
        /// <param name="path">The folder path used in the query.</param>
        public FolderParameter(string path) => this.Value = path;

        /// <summary>
        /// Returns the query parameter in its <see cref="Uri"/> query form.
        /// </summary>
        /// <remarks>
        ///     The folder path will always be URL-encoded in the  process.
        /// </remarks>
        /// <returns>A string, URL-encoded query parameter.</returns>
        public string AsString() => string.Format("{0}={1}", this.Key, WebUtility.UrlEncode(this.Value));
    }
}
