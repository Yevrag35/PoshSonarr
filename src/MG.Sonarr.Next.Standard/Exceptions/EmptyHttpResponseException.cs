using System.Diagnostics;
using System.Net;

#nullable enable

namespace MG.Sonarr.Next.Exceptions
{
    /// <summary>
    /// An exception that is throw when an <see cref="HttpRequestMessage"/> did not indicate an error
    /// <see cref="HttpStatusCode"/>, but no response content was returned when one was expected.
    /// </summary>
    public sealed class EmptyHttpResponseException : PoshSonarrException, IHttpRequestUri
    {
        const string MSG = "An empty response was received when there should have been one.";

        /// <summary>
        /// Gets the URL of the request that generated the exception, if available.
        /// </summary>
        public string? Url { get; }

        /// <inheritdoc/>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string? IHttpRequestUri.RequestUri =>  this.Url;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmptyHttpResponseException"/> class with the 
        /// specified URL that the exception occurred from.
        /// </summary>
        /// <param name="url">The <see cref="string"/> url of the request.</param>
        public EmptyHttpResponseException(string? url) : base(MSG)
        {
            this.Url = url;
        }
    }
}
