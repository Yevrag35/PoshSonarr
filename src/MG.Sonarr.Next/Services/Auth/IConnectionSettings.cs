namespace MG.Sonarr.Next.Services.Auth;

/// <summary>
/// Defines connection settings used to communicate with a Sonarr server instance.
/// </summary>
public interface IConnectionSettings
{
	/// <summary>
	/// Gets the API key used to authenticate Sonarr API requests.
	/// </summary>
	/// <value>
	/// The API key abstraction for request authentication.
	/// </value>
	IApiKey ApiKey { get; }
	/// <summary>
	/// Gets or sets the authentication type used by the Sonarr server instance.
	/// </summary>
	/// <value>
	/// The configured Sonarr authentication type.
	/// </value>
	SonarrAuthType AuthType { get; set; }
	/// <summary>
	/// Gets a value that indicates whether API paths omit the <c>/api</c> prefix.
	/// </summary>
	/// <value>
	/// <see langword="true"/> when request paths should not be prefixed with <c>/api</c>; otherwise, <see langword="false"/>.
	/// </value>
	bool NoApiInPath { get; }
	/// <summary>
	/// Gets or sets the URL base that the Sonarr server's status reports.
	/// </summary>
	/// <remarks>
	/// The connection's test stores it. Sonarr accepts the URL base in any case, but it spells the URLs of its own pages,
	/// such as a log file's download URL, its own way, and the cookie of a forms login goes back only with a request
	/// whose path matches the cookie's path in case too. So downloads, and the forms login that they need, spell the URL
	/// base as the server does when it matches the path of <see cref="ServiceUri"/> without regard to case.
	/// </remarks>
	/// <value>
	/// The URL base as the server reports it, such as <c>/sonarr</c>, an empty string for a server without one, or
	/// <see langword="null"/> when no test has stored it.
	/// </value>
	string? ServerUrlBase { get; set; }
	/// <summary>
	/// Gets the base <see cref="Uri"/> of the Sonarr server instance.
	/// </summary>
	/// <value>
	/// The base service URI used for Sonarr requests.
	/// </value>
	Uri ServiceUri { get; }
	/// <summary>
	/// Gets a value that indicates whether client instances ignore certificate validation errors.
	/// </summary>
	/// <value>
	/// <see langword="true"/> when certificate validation errors are ignored; otherwise, <see langword="false"/>.
	/// </value>
	bool SkipCertValidation { get; }
	/// <summary>
	/// Gets the timeout applied to each API request.
	/// </summary>
	/// <value>
	/// The per-request timeout value. The default value is five minutes.
	/// </value>
	TimeSpan Timeout { get; }

	/// <summary>
	/// Attempts to retrieve a web proxy instance for HTTP request handlers.
	/// </summary>
	/// <param name="proxy">
	/// When this method returns, contains the resolved proxy when available; otherwise, <see langword="null"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> when a proxy is available; otherwise, <see langword="false"/>.
	/// </returns>
	bool TryGetProxy([NotNullWhen(true)] out IWebProxy? proxy);
}
