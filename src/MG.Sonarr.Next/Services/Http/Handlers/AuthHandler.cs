using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Http.Requests;
using System.Security.Cryptography;

namespace MG.Sonarr.Next.Services.Http.Handlers;

/// <summary>
/// Provides a delegating handler that adds the credentials that the server's authentication type needs to a download
/// request.
/// </summary>
/// <remarks>
/// <para>The handler reads <see cref="IConnectionSettings.AuthType"/> once, when its chain is built. For
/// <see cref="SonarrAuthType.None"/> and <see cref="SonarrAuthType.External"/>, it passes every request through
/// unchanged. For <see cref="SonarrAuthType.Basic"/> and <see cref="SonarrAuthType.Forms"/>, it takes the credentials
/// from the request, which <see cref="Clients.ISonarrDownloadClient"/> sends when a download is given a
/// <see cref="NetworkCredential"/>.</para>
/// <para>A request without credentials goes out as it is, since Sonarr serves a client without a login when its
/// authentication isn't required for the client's address. When Sonarr answers it with a 401 or sends it to the login
/// page, the handler fails it with a <see cref="SonarrHttpException"/> whose status is 401.</para>
/// <para>For Basic authentication, the handler adds an <c>Authorization: Basic</c> header. For forms authentication, it
/// posts the credentials to the login page under the URL base, and the primary handler keeps the login's cookie in the
/// connection's shared <see cref="CookieContainer"/>, which adds it to every later request. The handler logs in only
/// when the container holds no cookie for the request's URL, and when Sonarr answers a request with a 401 or sends it
/// to the login page, it logs in again and resends the request once, to its original URL.</para>
/// <para>The login page's URL spells the URL base as <see cref="IConnectionSettings.ServerUrlBase"/> does when the two
/// match without regard to case, as the download client spells its URLs, so the login's cookie, whose path is the URL
/// base, goes back with the downloads.</para>
/// <para>The handler keeps no per-request state in its fields. Concurrent requests that find no cookie can each log
/// in.</para>
/// </remarks>
public sealed class AuthHandler : DelegatingHandler
{
	private readonly SonarrAuthType _authType;
	private readonly CookieContainer _cookies;
	private readonly Uri _loginUrl;

	/// <summary>
	/// Initializes a new <see cref="AuthHandler"/> instance with the authentication type and URL base of the specified
	/// connection settings and the specified cookie container.
	/// </summary>
	/// <param name="settings">The connection settings that supply the server's authentication type and the URL base of
	/// its login page.</param>
	/// <param name="cookies">The cookie container that the connection's primary handlers share, which holds the forms
	/// login's cookie.</param>
	public AuthHandler(IConnectionSettings settings, CookieContainer cookies)
	{
		_authType = settings.AuthType;
		_cookies = cookies;
		_loginUrl = CreateLoginUrl(settings.ServiceUri, settings.GetUrlBase());
	}

	/// <summary>
	/// Adds the credentials that the server needs to the request, sends it to the inner handler, and blocks until its
	/// response arrives.
	/// </summary>
	/// <remarks>
	/// The method blocks on <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>, so the inner handlers run on
	/// their asynchronous path.
	/// </remarks>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="cancellationToken">A token that cancels the operation.</param>
	/// <returns>The HTTP response message from the inner handler.</returns>
	/// <exception cref="SonarrHttpException">Thrown when Sonarr answers a request without credentials with a 401 or its
	/// login page, when a forms login fails, or when Sonarr sends the request to its login page again after a new
	/// login.</exception>
	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return this.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
	}

	/// <summary>
	/// Adds the credentials that the server needs to the request and sends it to the inner handler asynchronously.
	/// </summary>
	/// <remarks>
	/// Every failure faults the returned task, and none throws before the method returns.
	/// </remarks>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="cancellationToken">A token that cancels the operation.</param>
	/// <returns>A task that completes with the HTTP response message from the inner handler.</returns>
	/// <exception cref="SonarrHttpException">Thrown when Sonarr answers a request without credentials with a 401 or its
	/// login page, when a forms login fails, or when Sonarr sends the request to its login page again after a new
	/// login.</exception>
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (_authType is not (SonarrAuthType.Basic or SonarrAuthType.Forms))
		{
			return base.SendAsync(request, cancellationToken);
		}

		NetworkCredential? credentials = (request as AuthedRequestMessage)?.Credentials;
		if (credentials is null)
		{
			return this.SendWithoutCredentialsAsync(request, cancellationToken);
		}

		if (_authType == SonarrAuthType.Basic)
		{
			SetBasicHeader(request, credentials);
			return base.SendAsync(request, cancellationToken);
		}

		return this.SendWithFormsLoginAsync(request, credentials, cancellationToken);
	}

	/// <summary>
	/// Sends a request that carries no credentials to the inner handler as it is, and fails it when Sonarr turns it away
	/// for want of a login.
	/// </summary>
	/// <remarks>
	/// Sonarr serves the request when its authentication isn't required for the client's address. Otherwise, a Basic
	/// server answers it with a 401, and a forms server sends it to the login page, which answers with a success that
	/// holds the page rather than the file.
	/// </remarks>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="token">A token that cancels the operation.</param>
	/// <returns>A task that completes with the HTTP response message from the inner handler.</returns>
	/// <exception cref="SonarrHttpException">Thrown when Sonarr answers the request with a 401 or its login
	/// page.</exception>
	private async Task<HttpResponseMessage> SendWithoutCredentialsAsync(HttpRequestMessage request, CancellationToken token)
	{
		HttpResponseMessage response = await base.SendAsync(request, token).ConfigureAwait(false);
		if (!this.IsLoginPage(response))
		{
			return response;
		}

		SonarrHttpException ex = CreateUnauthorized(request, response, _authType == SonarrAuthType.Basic
			? "The Sonarr server requires Basic authentication and no credentials were provided."
			: "The Sonarr server requires Forms-based authentication and no credentials were provided.");

		response.Dispose();
		throw ex;
	}

	/// <summary>
	/// Determines whether a response means that Sonarr turned the request away for want of a login.
	/// </summary>
	/// <param name="response">The HTTP response message to check.</param>
	/// <returns><see langword="true"/> if the response's status is 401 Unauthorized, or if its request ended up at the
	/// login page's path, compared ordinally and ignoring case; otherwise, <see langword="false"/>.</returns>
	private bool IsLoginPage(HttpResponseMessage response)
	{
		return response.StatusCode == HttpStatusCode.Unauthorized
			|| (response.RequestMessage?.RequestUri is Uri landedOn
				&& landedOn.AbsolutePath.Equals(_loginUrl.AbsolutePath, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Creates the exception that fails a request for want of a login, with the response's status set to 401
	/// Unauthorized.
	/// </summary>
	/// <remarks>
	/// A request that Sonarr sends to its login page ends with the page's own 200 OK, which would read as a success, so
	/// the method sets the status before the exception copies it. The caller disposes the response.
	/// </remarks>
	/// <param name="request">The request that failed.</param>
	/// <param name="response">The response that turned the request away.</param>
	/// <param name="message">The message of the <see cref="UnauthorizedAccessException"/> that the exception
	/// wraps.</param>
	/// <returns>The exception, whose status code is 401.</returns>
	private static SonarrHttpException CreateUnauthorized(HttpRequestMessage request, HttpResponseMessage response, string message)
	{
		response.StatusCode = HttpStatusCode.Unauthorized;
		response.ReasonPhrase = nameof(HttpStatusCode.Unauthorized);
		return new SonarrHttpException(request, response, ErrorCollection.Empty, new UnauthorizedAccessException(message));
	}

	#region BASIC AUTH
	private const string BASIC = "Basic";
	private const int MAX_STACKALLOC = 256;

	/// <summary>
	/// Sets a request's <c>Authorization</c> header to Basic, with the specified credentials' user name and password
	/// encoded as UTF-8 and then Base64.
	/// </summary>
	/// <remarks>
	/// The UTF-8 bytes go in a stack buffer when they fit in 256 bytes, and in a pooled buffer otherwise. Since they hold
	/// the password in plain text, the method clears the buffer before it returns, whether or not the encoding
	/// succeeded.
	/// </remarks>
	/// <param name="request">The request whose header is set.</param>
	/// <param name="credentials">The credentials to encode.</param>
	private static void SetBasicHeader(HttpRequestMessage request, NetworkCredential credentials)
	{
		string userName = credentials.UserName;
		string password = credentials.Password;
		int length = Encoding.UTF8.GetMaxByteCount(userName.Length) + 1 + Encoding.UTF8.GetMaxByteCount(password.Length);

		ReadOnlySpan<byte> colon = ":"u8;
		using RentedBuffer<byte> buffer = RentedBuffer.Rent<byte>(length <= MAX_STACKALLOC ? stackalloc byte[length] : length);
		Span<byte> utf8Bytes = buffer.Span;
		try
		{
			unsafe
			{
				// SAFETY: utf8Bytes holds at least length bytes: the most that userName can encode to, one for the colon,
				// and the most that password can. So the user name leaves room for the colon after it, the password's
				// bytes fit after that, and written never passes the length of utf8Bytes.
				int written = Encoding.UTF8.GetBytes(userName, utf8Bytes);
				colon.UnsafeCopy(utf8Bytes.UnsafeSlice(written));
				written += colon.Length;
				written += Encoding.UTF8.GetBytes(password, utf8Bytes.UnsafeSlice(written));

				string base64Auth = Convert.ToBase64String(utf8Bytes.UnsafeSlice(0, written));
				request.Headers.Authorization = new AuthenticationHeaderValue(BASIC, base64Auth);
			}
		}
		finally
		{
			// The bytes hold the password in plain text, and the stack frame or the pool keeps them after this call.
			CryptographicOperations.ZeroMemory(utf8Bytes);
		}
	}

	#endregion

	#region FORMS AUTH
	private const string LOGIN_FAILED = "loginFailed=true";
	private const string PASSWORD = "password";
	private const string USERNAME = "username";

	/// <summary>
	/// Sends a request to the inner handler with the cookie of a forms login, logging in first when there's no cookie for
	/// its URL, and logging in again and resending it once when Sonarr answers with its login page.
	/// </summary>
	/// <remarks>
	/// Following a redirect to the login page changes the request's URL, so the method puts the original URL back before
	/// it resends the request.
	/// </remarks>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="credentials">The credentials to log in with.</param>
	/// <param name="token">A token that cancels the operation.</param>
	/// <returns>A task that completes with the HTTP response message from the inner handler.</returns>
	/// <exception cref="SonarrHttpException">Thrown when a login fails, or when Sonarr sends the resent request to its
	/// login page again.</exception>
	private async Task<HttpResponseMessage> SendWithFormsLoginAsync(HttpRequestMessage request, NetworkCredential credentials, CancellationToken token)
	{
		// The primary handler keeps the login's cookie in the shared container and adds it to every later request.
		Uri requestUri = request.RequestUri!;
		if (_cookies.GetCookies(requestUri).Count == 0)
		{
			await this.LoginAsync(credentials, token).ConfigureAwait(false);
		}

		HttpResponseMessage response = await base.SendAsync(request, token).ConfigureAwait(false);
		if (!this.IsLoginPage(response))
		{
			return response;
		}

		// The session ended, so Sonarr sent the request to its login page. Log in again, and resend the request to the URL
		// that the redirect replaced.
		response.Dispose();
		await this.LoginAsync(credentials, token).ConfigureAwait(false);
		request.RequestUri = requestUri;

		response = await base.SendAsync(request, token).ConfigureAwait(false);
		if (this.IsLoginPage(response))
		{
			SonarrHttpException ex = CreateUnauthorized(request, response,
				"The Sonarr server sent the request to its login page again after a login.");

			response.Dispose();
			throw ex;
		}

		return response;
	}

	/// <summary>
	/// Posts the specified credentials to Sonarr's login page, so that the shared cookie container receives the login's
	/// cookie, and checks that Sonarr accepted the login.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The primary handler follows the redirect that answers the login. Sonarr sends a login that fails back to its login
	/// page with <c>loginFailed=true</c> in the query, and one that succeeds to the URL base, which it sends to the login
	/// page in turn when the request for it doesn't carry the login's cookie, unless its authentication isn't required
	/// for the client's address. So a login whose redirect ends with a success status anywhere but the login page has a
	/// cookie that Sonarr accepts, or doesn't need one.
	/// </para>
	/// <para>
	/// The form holds the user name as it is and the password in plain text, URL-encoded as the <c>username</c> and
	/// <c>password</c> fields.
	/// </para>
	/// </remarks>
	/// <param name="credentials">The credentials to log in with.</param>
	/// <param name="token">A token that cancels the operation.</param>
	/// <returns>A task that completes when the login has succeeded.</returns>
	/// <exception cref="SonarrHttpException">Thrown, with the status 401, when the login's redirect lands on a URL that
	/// reports a failed login or on the login page, and with the login's own status when it ends with an error
	/// status.</exception>
	private async Task LoginAsync(NetworkCredential credentials, CancellationToken token)
	{
		using HttpRequestMessage request = new(HttpMethod.Post, _loginUrl);
		request.Content = new FormUrlEncodedContent(
		[
			new KeyValuePair<string, string>(USERNAME, credentials.UserName),
			new KeyValuePair<string, string>(PASSWORD, credentials.Password),
		]);

		using HttpResponseMessage response = await base.SendAsync(request, token).ConfigureAwait(false);

		if (response.RequestMessage?.RequestUri?.Query.Contains(LOGIN_FAILED, StringComparison.OrdinalIgnoreCase) ?? false)
		{
			throw CreateUnauthorized(request, response, "The username or password is incorrect.");
		}

		if (!response.IsSuccessStatusCode)
		{
			throw new SonarrHttpException(request, response, ErrorCollection.Empty,
				new UnauthorizedAccessException("The Sonarr server didn't accept the login."));
		}

		if (this.IsLoginPage(response))
		{
			throw CreateUnauthorized(request, response, "The Sonarr server sent the login back to its login page.");
		}
	}

	/// <summary>
	/// Creates the URL of Sonarr's login page under the specified URL base, with a return URL of the URL base.
	/// </summary>
	/// <param name="serviceUri">The connection's URL, whose scheme and server the login page's URL takes.</param>
	/// <param name="urlBase">The URL base, with its leading slash and without a trailing one, or an empty string.</param>
	/// <returns>The absolute URL <c>&lt;base&gt;/login?returnUrl=&lt;base&gt;/</c> on the server of
	/// <paramref name="serviceUri"/>.</returns>
	private static Uri CreateLoginUrl(Uri serviceUri, string urlBase)
	{
		// Sonarr serves its login page under the URL base, and the redirect after a login goes back to the URL base.
		return new Uri(serviceUri, $"{urlBase}/login?returnUrl={urlBase}/");
	}

	#endregion
}
