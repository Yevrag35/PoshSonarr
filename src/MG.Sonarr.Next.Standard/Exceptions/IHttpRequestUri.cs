#nullable enable

namespace MG.Sonarr.Next.Exceptions
{
    /// <summary>
    /// Represents an object that has defined a request URI for already processed HTTP request.
    /// </summary>
    public interface IHttpRequestUri
    {
        /// <summary>
        /// Gets the URI of the request associated with this instance.
        /// </summary>
        string? RequestUri { get; }
    }
}
