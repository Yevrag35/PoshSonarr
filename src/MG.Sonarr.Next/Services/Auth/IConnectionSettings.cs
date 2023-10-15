using System.Net;

namespace MG.Sonarr.Next.Services.Auth
{
    /// <summary>
    /// An interface exposing connection properties and methods for connecting to a Sonarr server instance.
    /// </summary>
    public interface IConnectionSettings
    {
        /// <summary>
        /// The API key to authenticate Sonarr requests.
        /// </summary>
        IApiKey ApiKey { get; }
        /// <summary>
        /// The authentication type of the Sonarr server instance.
        /// </summary>
        SonarrAuthType AuthType { get; set; }
        /// <summary>
        /// Indicates that "/api" should not be added to all API request paths.
        /// </summary>
        bool NoApiInPath { get; }
        /// <summary>
        /// The base <see cref="Uri"/> of the Sonarr server instance.
        /// </summary>
        Uri ServiceUri { get; }
        /// <summary>
        /// Indicates that the <see cref="HttpClient"/> instances should ignore certificate validation
        /// errors.
        /// </summary>
        bool SkipCertValidation { get; }
        /// <summary>
        /// The timeout for each API request. Defaults to 5 minutes.
        /// </summary>
        TimeSpan Timeout { get; }

        /// <summary>
        /// Attempts to retrieve a web proxy instance for use by the <see cref="HttpClientHandler"/>.
        /// </summary>
        /// <param name="proxy"></param>
        /// <returns></returns>
        bool TryGetProxy([NotNullWhen(true)] out IWebProxy? proxy);
    }
}
