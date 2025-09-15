using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Shell.Exceptions;
using System.Net;

namespace MG.Sonarr.Next.Shell.Settings
{
    /// <summary>
    /// A class for setting the connection properties for all Sonarr-based <see cref="HttpClient"/> 
    /// implementations.
    /// </summary>
    public sealed class ConnectionSettings : IConnectionSettings
    {
        /// <summary>
        /// Gets or sets the API key used for authenticating requests to external services.
        /// </summary>
        internal ApiKey Key { get; set; }

        /// <inheritdoc/>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        IApiKey IConnectionSettings.ApiKey => this.Key;

        /// <summary>
        /// Gets or sets the authentication type used to connect to the Sonarr service.
        /// </summary>
        public SonarrAuthType AuthType { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the API segment is excluded from the request path.
        /// </summary>
        public bool NoApiInPath { get; set; }
        /// <summary>
        /// Gets or sets the base <see cref="Uri"/> of the Sonarr service.
        /// </summary>
        public Uri ServiceUri { get; set; } = null!;
        /// <summary>
        /// Gets or sets a value indicating whether server certificate validation should be skipped during secure
        /// connections.
        /// </summary>
        /// <remarks>Skipping certificate validation can expose the application to security risks, such as
        /// man-in-the-middle attacks. Use this property only in trusted environments, such as development or
        /// testing.</remarks>
        public bool SkipCertValidation { get; set; }
        /// <summary>
        /// Gets or sets the URI of the proxy server to be used for network requests.
        /// </summary>
        public string? ProxyUri { get; set; }
        /// <summary>
        /// Gets or sets the credentials used to authenticate with the proxy server.
        /// </summary>
        internal ProxyCredential? ProxyCredential { get; set; }
        /// <summary>
        /// Gets or sets the maximum duration to wait for the operation to complete before timing out.
        /// </summary>
        /// <remarks>The default timeout is five minutes. Adjust this value to control how long the
        /// operation is allowed to run before it is canceled. Setting a very short timeout may cause operations to fail
        /// prematurely, while a very long timeout may delay error detection.</remarks>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5d);

        /// <summary>
        /// Attempts to retrieve a configured proxy for HTTP requests.
        /// </summary>
        /// <remarks>This method does not throw an exception if no proxy is configured. Use the return
        /// value to determine whether a proxy was found before using the <paramref name="proxy"/> parameter.</remarks>
        /// <param name="proxy">When this method returns, contains the <see cref="IWebProxy"/> instance representing the configured proxy if
        /// one is available; otherwise, <see langword="null"/>.</param>
        /// <returns>true if a proxy is configured and returned in <paramref name="proxy"/>; otherwise, false.</returns>
        public bool TryGetProxy([NotNullWhen(true)] out IWebProxy? proxy)
        {
            proxy = null;
            if (this.ProxyUri is not null)
            {
                proxy = new WebProxy(this.ProxyUri, true, null, this.ProxyCredential);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Validates the current configuration by ensuring that all required properties are set and have valid values.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if the base service URI is not provided.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the timeout value is less than or equal to zero.</exception>
        /// <inheritdoc cref="ApiKey.Validate" path="/exception"/>
        internal void Validate()
        {
            this.Key.Validate();
            if (this.ServiceUri is null)
            {
                throw new ArgumentNullException("A base URL must be provided.");
            }

            if (this.Timeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("The timeout must be greater than zero.");
            }
        }
    }
}
