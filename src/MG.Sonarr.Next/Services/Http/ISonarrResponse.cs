using System.Net;

namespace MG.Sonarr.Next.Services.Http
{
    /// <summary>
    /// An interface that represents HTTP responses from Sonarr API endpoints.
    /// </summary>
    public interface ISonarrResponse
    {
        /// <summary>
        /// Gets the error record of the response, if any.
        /// </summary>
        SonarrErrorRecord? Error { get; }

        /// <summary>
        /// Gets whether the response is considered an error.
        /// </summary>
        [MemberNotNullWhen(true, nameof(Error))]
        bool IsError { get; }
        /// <summary>
        /// Gets the HTTP status code of the response.
        /// </summary>
        HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Gets the <see cref="string"/>-form of the URL that the response's request was sent to.
        /// </summary>
        string RequestUrl { get; }
    }
    /// <summary>
    /// An interface for <see cref="ISonarrResponse"/> implementations that were measured and provide a
    /// <see cref="TimeSpan"/> of the elapsed time.
    /// </summary>
    public interface ISonarrTimedResponse : ISonarrResponse
    {
        /// <summary>
        /// Gets the amount of time between when the request was sent and when the response was received.
        /// </summary>
        TimeSpan Elapsed { get; }
        /// <summary>
        /// Gets a scoped <see cref="IServiceProvider"/> for use by code that is using this response.
        /// </summary>
        IServiceProvider Services { get; }
    }
}
