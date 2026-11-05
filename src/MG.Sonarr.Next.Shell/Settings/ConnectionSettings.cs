using MG.Sonarr.Next.Services.Auth;
using System.Net;

namespace MG.Sonarr.Next.Shell.Settings;

/// <summary>
/// Represents the settings that a connection to Sonarr is built with: the URL, the API key, the timeout, and how the
/// module's HTTP client reaches the server.
/// </summary>
/// <remarks>
/// <para>
/// <c>Connect-SonarrInstance</c> and <c>Test-SonarrHost</c> fill an instance from their parameters, and the connection's
/// services read it as <see cref="IConnectionSettings"/>. The HTTP handlers read it when their chain is built, so a
/// change after that reaches a handler only once the chain is built again.
/// </para>
/// <para>
/// The instance isn't thread-safe. A connection, which every runspace shares, writes only
/// <see cref="AuthType"/> after it's built.
/// </para>
/// </remarks>
public sealed class ConnectionSettings : IConnectionSettings
{
	/// <summary>
	/// Gets or sets the API key that every request sends in its <c>X-Api-Key</c> header.
	/// </summary>
	/// <value>The key from <c>-ApiKey</c>. The default holds no key.</value>
	internal ApiKey Key { get; set; }

	/// <summary>
	/// Gets the API key that every request sends.
	/// </summary>
	/// <value>The key that <see cref="Key"/> holds.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	IApiKey IConnectionSettings.ApiKey => this.Key;

	/// <summary>
	/// Gets or sets the authentication type that the Sonarr server requires.
	/// </summary>
	/// <remarks>
	/// The connection's test of the URL and key stores the type that the server's status names, and a download adds
	/// credentials when the type calls for them.
	/// </remarks>
	/// <value>The server's authentication type. The default is <see cref="SonarrAuthType.None"/>.</value>
	public SonarrAuthType AuthType { get; set; }
	/// <summary>
	/// Gets or sets a value that indicates whether request paths leave out the <c>/api</c> prefix, for a reverse proxy
	/// that removes it.
	/// </summary>
	/// <value><see langword="true"/> to prefix paths with <c>/v3</c> instead of <c>/api/v3</c>; otherwise,
	/// <see langword="false"/>. The default is <see langword="false"/>.</value>
	public bool NoApiInPath { get; set; }
	/// <summary>
	/// Gets or sets the base URL of the Sonarr server.
	/// </summary>
	/// <remarks>
	/// The URL's path, if any, is the URL base that every request's path follows.
	/// </remarks>
	/// <value>The absolute URL from <c>-Url</c>, which must be set before the connection is built.</value>
	public Uri ServiceUri { get; set; } = null!;
	/// <summary>
	/// Gets or sets a value that indicates whether the client accepts any server certificate.
	/// </summary>
	/// <remarks>
	/// Accepting any certificate exposes the connection to man-in-the-middle attacks, so it suits only a trusted
	/// network, such as one for development or testing.
	/// </remarks>
	/// <value><see langword="true"/> to skip the validation of the server's certificate; otherwise,
	/// <see langword="false"/>. The default is <see langword="false"/>.</value>
	public bool SkipCertValidation { get; set; }
	/// <summary>
	/// Gets or sets the URL of the proxy server that requests go through.
	/// </summary>
	/// <value>The proxy's URL, or <see langword="null"/> to send requests without a proxy. The default is
	/// <see langword="null"/>.</value>
	public string? ProxyUri { get; set; }
	/// <summary>
	/// Gets or sets the credentials that the proxy server is authenticated with.
	/// </summary>
	/// <value>The proxy's credentials, or <see langword="null"/> for none. The default is <see langword="null"/>.</value>
	internal ProxyCredential? ProxyCredential { get; set; }
	/// <summary>
	/// Gets or sets how long the client waits for a request to complete before it times out.
	/// </summary>
	/// <remarks>
	/// The client accepts a timeout greater than zero and at most <see cref="int.MaxValue"/> milliseconds, and refuses
	/// any other value when it's built.
	/// </remarks>
	/// <value>The timeout of each request. The default is five minutes.</value>
	public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5d);

	/// <summary>
	/// Gets the proxy that requests go through, when <see cref="ProxyUri"/> names one.
	/// </summary>
	/// <remarks>
	/// The proxy is created on each call, and it bypasses local addresses.
	/// </remarks>
	/// <param name="proxy">When the method returns, the proxy for <see cref="ProxyUri"/>, with the proxy's credentials,
	/// if any; otherwise, <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if <see cref="ProxyUri"/> isn't <see langword="null"/>; otherwise,
	/// <see langword="false"/>.</returns>
	/// <exception cref="UriFormatException">Thrown when <see cref="ProxyUri"/> isn't a valid URL.</exception>
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
	/// Checks that the settings hold an API key, a URL, and a timeout greater than zero.
	/// </summary>
	/// <exception cref="MG.Sonarr.Next.Exceptions.Shell.InvalidApiKeyException">Thrown when the API key is missing,
	/// empty, or only white space.</exception>
	/// <exception cref="ArgumentNullException">Thrown when <see cref="ServiceUri"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="Timeout"/> is zero or less.</exception>
	internal void Validate()
	{
		this.Key.Validate();
		if (this.ServiceUri is null)
		{
			throw new ArgumentNullException(nameof(this.ServiceUri), "A base URL must be provided.");
		}

		if (this.Timeout <= TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(this.Timeout), this.Timeout, "The timeout must be greater than zero.");
		}
	}
}
