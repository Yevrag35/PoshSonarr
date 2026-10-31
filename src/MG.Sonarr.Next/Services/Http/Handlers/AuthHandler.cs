using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Http.Requests;
using System.Net;
using System.Net.Http.Headers;
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
/// from the request, which <see cref="ISonarrDownloadClient"/> sends when a download is given a
/// <see cref="NetworkCredential"/>, and fails the request with a <see cref="SonarrHttpException"/> when there are
/// none.</para>
/// <para>For Basic authentication, the handler adds an <c>Authorization: Basic</c> header. For forms authentication, it
/// posts the credentials to the login page under the URL base, and the primary handler keeps the login's cookie in the
/// connection's shared <see cref="CookieContainer"/>, which adds it to every later request. The handler logs in only
/// when the container holds no cookie for the request's URL, and when Sonarr answers a request with a 401 or sends it
/// to the login page, it logs in again and resends the request once, to its original URL.</para>
/// <para>The handler keeps no per-request state in its fields. Concurrent requests that find no cookie can each log
/// in.</para>
/// </remarks>
public sealed class AuthHandler : DelegatingHandler
{
	/// <summary>
	/// The key of a request option for a download's credentials.
	/// </summary>
	/// <remarks>
	/// The handler takes the credentials from the request message itself and doesn't read this option.
	/// </remarks>
	public static readonly HttpRequestOptionsKey<NetworkCredential?> CredentialKey = new("Credentials");

	readonly SonarrAuthType _authType;
	readonly CookieContainer _cookies;
	readonly Uri _loginUrl;

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
		_loginUrl = CreateLoginUrl(settings.ServiceUri);
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
	/// <exception cref="SonarrHttpException">Thrown when the server needs Basic or forms authentication and the request
	/// carries no credentials, when a forms login fails or returns no cookie, or when Sonarr sends the request to its
	/// login page again after a new login.</exception>
	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return this.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
	}

	/// <summary>
	/// Adds the credentials that the server needs to the request and sends it to the inner handler asynchronously.
	/// </summary>
	/// <remarks>
	/// For Basic authentication, a request without credentials throws before the method returns. For forms
	/// authentication, the returned task faults instead.
	/// </remarks>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="cancellationToken">A token that cancels the operation.</param>
	/// <returns>A task that completes with the HTTP response message from the inner handler.</returns>
	/// <exception cref="SonarrHttpException">Thrown when the server needs Basic or forms authentication and the request
	/// carries no credentials, when a forms login fails or returns no cookie, or when Sonarr sends the request to its
	/// login page again after a new login.</exception>
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		switch (_authType)
		{
			case SonarrAuthType.None:
			case SonarrAuthType.External:
				return base.SendAsync(request, cancellationToken);

			case SonarrAuthType.Basic:
				return this.DoBasicAuthProcedure(request, cancellationToken);

			case SonarrAuthType.Forms:
				return this.DoFormsAuthProcedure(request, cancellationToken);

			default:
				goto case SonarrAuthType.None;
		}
	}

	/// <summary>
	/// Attempts to get a request as an <see cref="AuthedRequestMessage"/> whose credentials aren't blank.
	/// </summary>
	/// <param name="request">The HTTP request message to check.</param>
	/// <param name="result">When this method returns, <paramref name="request"/> as an <see cref="AuthedRequestMessage"/>,
	/// or <see langword="null"/> when it isn't one or its credentials are blank.</param>
	/// <returns><see langword="true"/> if <paramref name="request"/> carries credentials that aren't blank; otherwise,
	/// <see langword="false"/>.</returns>
	private static bool TryGetAsAuthedRequest(HttpRequestMessage request, [NotNullWhen(true)] out AuthedRequestMessage? result)
	{
		result = null;
		if (request is not AuthedRequestMessage authedMsg || authedMsg.IsCredentialsBlank)
		{
			return false;
		}

		result = authedMsg;
		return true;
	}

	#region BASIC AUTH
	const string BASIC = "Basic";
	const int MAX_STACKALLOC = 256;

	/// <summary>
	/// Adds a Basic <c>Authorization</c> header to a request that carries credentials and sends it to the inner
	/// handler.
	/// </summary>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="token">A token that cancels the operation.</param>
	/// <returns>A task that completes with the HTTP response message from the inner handler.</returns>
	/// <exception cref="SonarrHttpException">Thrown when <paramref name="request"/> carries no credentials, or only blank
	/// ones.</exception>
	private Task<HttpResponseMessage> DoBasicAuthProcedure(HttpRequestMessage request, CancellationToken token)
	{
		if (!TryGetAsAuthedRequest(request, out AuthedRequestMessage? authedMsg))
		{
			throw new SonarrHttpException(request, null, ErrorCollection.Empty,
				new UnauthorizedAccessException("The Sonarr server requires Basic Authentication and no credentials were provided."));
		}

		AddBasicToRequest(authedMsg);
		return base.SendAsync(authedMsg, token);
	}

	/// <summary>
	/// Sets a request's <c>Authorization</c> header to Basic, with its credentials' user name and password encoded as
	/// UTF-8 and then Base64.
	/// </summary>
	/// <remarks>
	/// The UTF-8 bytes go in a stack buffer when they fit in 256 bytes, and in a pooled buffer otherwise. Since they hold
	/// the password in plain text, the method clears the buffer before it returns, whether or not the encoding
	/// succeeded.
	/// </remarks>
	/// <param name="request">The request whose credentials are encoded and whose header is set.</param>
	private static void AddBasicToRequest(AuthedRequestMessage request)
	{
		NetworkCredential creds = request.Credentials;
		string userName = creds.UserName;
		string password = creds.Password;
		int length = Encoding.UTF8.GetByteCount(userName) + 1 + Encoding.UTF8.GetByteCount(password);

		using RentedBuffer<byte> buffer = RentedBuffer.Rent<byte>(length <= MAX_STACKALLOC ? stackalloc byte[length] : length);
		Span<byte> utf8Bytes = buffer.Span;
		try
		{
			int written = Encoding.UTF8.GetBytes(userName, utf8Bytes);
			utf8Bytes[written++] = (byte)':';
			written += Encoding.UTF8.GetBytes(password, utf8Bytes[written..]);

			string base64Auth = Convert.ToBase64String(utf8Bytes[..written]);
			request.Headers.Authorization = new AuthenticationHeaderValue(BASIC, base64Auth);
		}
		finally
		{
			// The bytes hold the password in plain text, and the stack frame or the pool keeps them after this call.
			CryptographicOperations.ZeroMemory(utf8Bytes);
		}
	}

	#endregion

	#region FORMS AUTH
	const string LOGIN_FAILED = "loginFailed=true";

	/// <summary>
	/// Sends a request that carries credentials to the inner handler, logging in first when there's no cookie for its
	/// URL, and logging in again and resending it once when Sonarr answers with its login page.
	/// </summary>
	/// <remarks>
	/// Following a redirect to the login page changes the request's URL, so the method puts the original URL back before
	/// it resends the request.
	/// </remarks>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="token">A token that cancels the operation.</param>
	/// <returns>A task that completes with the HTTP response message from the inner handler.</returns>
	/// <exception cref="SonarrHttpException">Thrown when <paramref name="request"/> carries no credentials, or only blank
	/// ones, when a login fails or returns no cookie, or when Sonarr sends the resent request to its login page
	/// again.</exception>
	private async Task<HttpResponseMessage> DoFormsAuthProcedure(HttpRequestMessage request, CancellationToken token)
	{
		if (!TryGetAsAuthedRequest(request, out AuthedRequestMessage? authedMsg))
		{
			throw new SonarrHttpException(request, null, ErrorCollection.Empty,
				new UnauthorizedAccessException("The Sonarr server requires Forms-based authentication and no credentials were provided."));
		}

		// The primary handler keeps the login's cookie in the shared container and adds it to every later request.
		Uri requestUri = authedMsg.RequestUri!;
		if (_cookies.GetCookies(requestUri).Count == 0)
		{
			await this.LoginAsync(authedMsg, token).ConfigureAwait(false);
		}

		HttpResponseMessage response = await base.SendAsync(authedMsg, token).ConfigureAwait(false);
		if (!this.IsLoginPage(response))
		{
			return response;
		}

		// The session ended, so Sonarr sent the request to its login page. Log in again, and resend the request to the URL
		// that the redirect replaced.
		response.Dispose();
		await this.LoginAsync(authedMsg, token).ConfigureAwait(false);
		authedMsg.RequestUri = requestUri;

		response = await base.SendAsync(authedMsg, token).ConfigureAwait(false);
		if (this.IsLoginPage(response))
		{
			SonarrHttpException ex = new(authedMsg, response, ErrorCollection.Empty,
				new UnauthorizedAccessException("The Sonarr server sent the request to its login page again after a login."));

			response.Dispose();
			throw ex;
		}

		return response;
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
	/// Posts a request's credentials to Sonarr's login page, so that the shared cookie container receives the login's
	/// cookie.
	/// </summary>
	/// <param name="original">The request whose credentials are posted.</param>
	/// <param name="token">A token that cancels the operation.</param>
	/// <returns>A task that completes when the login has succeeded.</returns>
	/// <exception cref="SonarrHttpException">Thrown when <paramref name="original"/> can't give a login form, when the
	/// login's redirect lands on a URL that reports a failed login, or when the container holds no cookie for the login
	/// page afterward.</exception>
	private async Task LoginAsync(AuthedRequestMessage original, CancellationToken token)
	{
		using HttpRequestMessage request = new(HttpMethod.Post, _loginUrl);
		try
		{
			request.Content = original.GetLoginFormContent();
		}
		catch (UnauthorizedAccessException e)
		{
			throw new SonarrHttpException(original, null, ErrorCollection.Empty, e);
		}

		// The primary handler follows the redirect that answers the login, and stores the cookie that the redirect sets.
		// When the credentials are wrong, the redirect lands on the login page again.
		using HttpResponseMessage response = await base.SendAsync(request, token).ConfigureAwait(false);

		if (response.RequestMessage?.RequestUri?.Query.Contains(LOGIN_FAILED, StringComparison.OrdinalIgnoreCase) ?? false)
		{
			response.StatusCode = HttpStatusCode.Unauthorized;
			response.ReasonPhrase = response.StatusCode.ToString();
			throw new SonarrHttpException(request, response, ErrorCollection.Empty,
				new UnauthorizedAccessException("The username or password is incorrect."));
		}

		if (_cookies.GetCookies(_loginUrl).Count == 0)
		{
			throw new SonarrHttpException(request, response, ErrorCollection.Empty,
				new UnauthorizedAccessException("The Sonarr server didn't return a cookie for the login."));
		}
	}

	/// <summary>
	/// Creates the URL of Sonarr's login page under the URL base, with a return URL of the URL base.
	/// </summary>
	/// <param name="serviceUri">The connection's URL, whose path is the URL base.</param>
	/// <returns>The absolute URL <c>&lt;base&gt;/login?returnUrl=&lt;base&gt;/</c> on the server of
	/// <paramref name="serviceUri"/>.</returns>
	private static Uri CreateLoginUrl(Uri serviceUri)
	{
		// Sonarr serves its login page under the URL base, and the redirect after a login goes back to the URL base.
		string basePath = serviceUri.AbsolutePath.TrimEnd('/');
		return new Uri(serviceUri, $"{basePath}/login?returnUrl={basePath}/");
	}

	#endregion
}
