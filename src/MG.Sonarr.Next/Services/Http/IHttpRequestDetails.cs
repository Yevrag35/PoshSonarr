namespace MG.Sonarr.Next.Services.Http
{
    /// <summary>
    /// Provides details about an HTTP request, including the request method and URL.
    /// </summary>
    /// <remarks>Implementations of this interface expose information about the current HTTP request and may
    /// provide additional services via the <see cref="IServiceProvider"/> interface.</remarks>
    public interface IHttpRequestDetails : IServiceProvider
    {
        /// <summary>
        /// Gets the HTTP method used for the request, such as "GET", "POST", or "PUT".
        /// </summary>
        string RequestMethod { get; }
        /// <summary>
        /// Gets the URL of the request.
        /// </summary>
        string RequestUrl { get; }
    }
}
