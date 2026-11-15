namespace MG.Sonarr.Next.Services.Http.Requests;

/// <summary>
/// Represents a download request to a Sonarr server that can carry a user name and password, for a server that requires
/// Basic or forms authentication.
/// </summary>
/// <remarks>
/// <para>
///     The request can use the cookie from a forms login. A request without credentials goes out as it is, and succeeds
///     only when the server serves it without a login.
/// </para>
/// <para>
///     The handlers that build the request's URL leave it unchanged, so the caller passes the URL that the request
///     goes to.
/// </para>
/// </remarks>
internal sealed class AuthedRequestMessage : SonarrRequestMessage
{
	/// <summary>
	/// Gets the credentials that the request logs in with.
	/// </summary>
	/// <value>The credentials passed to the constructor, or <see langword="null"/> when it was given none.</value>
	public NetworkCredential? Credentials { get; }
	/// <summary>
	/// Gets a value that indicates whether the request tests the connection to the server.
	/// </summary>
	/// <value>Always <see langword="false"/>.</value>
	public override bool IsTest => false;

	/// <summary>
	/// Initializes a new <see cref="AuthedRequestMessage"/> instance with the specified HTTP method, URL, credentials,
	/// and scope factory.
	/// </summary>
	/// <param name="method">The request's HTTP method.</param>
	/// <param name="requestUri">The URL that the request goes to.</param>
	/// <param name="credentials">The credentials that the request logs in with, or <see langword="null"/> for none.</param>
	/// <param name="scopeFactory">The factory that the request opens its scope from.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="method"/> is null.</exception>
	/// <exception cref="UriFormatException">Thrown when <paramref name="requestUri"/> isn't a valid relative or absolute URL.</exception>
	public AuthedRequestMessage(HttpMethod method, string requestUri, NetworkCredential? credentials, IServiceScopeFactory scopeFactory)
		: base(method, requestUri, scopeFactory)
	{
		this.Credentials = credentials;
	}
}
