using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace MG.Sonarr.Next.Services.Http.Requests;

/// <summary>
/// Represents a request to a Sonarr server that carries a user name and password, for a server that requires Basic or
/// forms authentication.
/// </summary>
/// <remarks>
/// <para>
///     The request can use the cookie from a forms login. Its credentials can be blank, in which case the request
///     authenticates only if the server requires no login.
/// </para>
/// <para>
///     The handlers that build the request's URL leave it unchanged, so the caller passes the URL that the request
///     goes to.
/// </para>
/// </remarks>
internal sealed class AuthedRequestMessage : SonarrRequestMessage
{
	const string PASSWORD = "password";
	const string USERNAME = "username";
	static readonly NetworkCredential _blankCreds = new();

	/// <summary>
	/// Gets the credentials that the request logs in with.
	/// </summary>
	/// <remarks>
	/// Every request created without credentials shares one blank instance, so callers don't change the instance that
	/// the property returns.
	/// </remarks>
	/// <value>The credentials passed to the constructor, or a blank set when it was given none.</value>
	public NetworkCredential Credentials { get; }
	/// <summary>
	/// Gets a value that indicates whether the request's credentials lack a user name or a password.
	/// </summary>
	/// <value>
	///     <see langword="true"/> if the user name of <see cref="Credentials"/> is
	///     <see langword="null"/>, empty, or white space, or if the password is blank; otherwise, <see langword="false"/>.
	/// </value>
	public bool IsCredentialsBlank => string.IsNullOrWhiteSpace(this.Credentials.UserName)
									  ||
									  this.Credentials.SecurePassword.Length == 0;
	/// <summary>
	/// Gets a value that indicates whether the request tests the connection to the server.
	/// </summary>
	/// <value>Always <see langword="false"/>.</value>
	public override bool IsTest => false;
	/// <summary>
	/// Gets a value that indicates whether the request can authenticate with a login cookie.
	/// </summary>
	/// <value>Always <see langword="true"/>.</value>
	public override bool CanUseCookieAuthentication => true;

	/// <summary>
	/// Initializes a new <see cref="AuthedRequestMessage"/> instance with the specified HTTP method, URL, credentials,
	/// and scope factory.
	/// </summary>
	/// <param name="method">The request's HTTP method.</param>
	/// <param name="requestUri">The URL that the request goes to.</param>
	/// <param name="credentials">The credentials that the request logs in with, or <see langword="null"/> for blank ones.</param>
	/// <param name="scopeFactory">The factory that the request opens its scope from.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="method"/> is null.</exception>
	/// <exception cref="UriFormatException">Thrown when <paramref name="requestUri"/> isn't a valid relative or absolute URL.</exception>
	public AuthedRequestMessage(HttpMethod method, string requestUri, NetworkCredential? credentials, IServiceScopeFactory scopeFactory)
		: base(method, requestUri, scopeFactory)
	{
		this.Credentials = credentials ?? _blankCreds;
	}

	/// <summary>
	/// Creates the form body that posts the request's credentials to Sonarr's login page.
	/// </summary>
	/// <remarks>
	/// The body holds the password in plain text, URL-encoded as the <c>password</c> field beside the <c>username</c>
	/// field.
	/// </remarks>
	/// <returns>A new <c>application/x-www-form-urlencoded</c> body with the user name and password.</returns>
	/// <exception cref="UnauthorizedAccessException">Thrown when <see cref="IsCredentialsBlank"/> is true, with a message that says no credentials were given.</exception>
	public HttpContent GetLoginFormContent()
	{
		return !this.IsCredentialsBlank
			? (HttpContent)new FormUrlEncodedContent(GetFormContents(this.Credentials))
			: throw new UnauthorizedAccessException("No credentials were given.");
	}

	/// <summary>
	/// Returns the login form's fields for the specified credentials.
	/// </summary>
	/// <param name="credential">The credentials whose user name and password the fields hold.</param>
	/// <returns>An array containing the <c>username</c> field, then the <c>password</c> field.</returns>
	private static KeyValuePair<string, string>[] GetFormContents(NetworkCredential credential)
	{
		return
		[
			new KeyValuePair<string, string>(USERNAME, credential.UserName),
			new KeyValuePair<string, string>(PASSWORD, credential.Password),
		];
	}
}
