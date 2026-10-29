using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Http.Requests;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace MG.Sonarr.Next.Services.Http.Handlers;

public sealed class AuthHandler : DelegatingHandler
{
	public static readonly HttpRequestOptionsKey<NetworkCredential?> CredentialKey = new("Credentials");

	readonly SonarrAuthType _authType;
	readonly CookieContainer _cookies;
	readonly Uri _loginUrl;

	public AuthHandler(IConnectionSettings settings, CookieContainer cookies)
	{
		_authType = settings.AuthType;
		_cookies = cookies;
		_loginUrl = CreateLoginUrl(settings.ServiceUri);
	}

	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return this.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
	}

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

	private bool IsLoginPage(HttpResponseMessage response)
	{
		return response.StatusCode == HttpStatusCode.Unauthorized
			|| (response.RequestMessage?.RequestUri is Uri landedOn
				&& landedOn.AbsolutePath.Equals(_loginUrl.AbsolutePath, StringComparison.OrdinalIgnoreCase));
	}

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

	private static Uri CreateLoginUrl(Uri serviceUri)
	{
		// Sonarr serves its login page under the URL base, and the redirect after a login goes back to the URL base.
		string basePath = serviceUri.AbsolutePath.TrimEnd('/');
		return new Uri(serviceUri, $"{basePath}/login?returnUrl={basePath}/");
	}

	#endregion
}
