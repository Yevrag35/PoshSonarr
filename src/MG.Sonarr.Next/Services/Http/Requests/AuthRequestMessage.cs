using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace MG.Sonarr.Next.Services.Http.Requests;

internal sealed class AuthedRequestMessage : SonarrRequestMessage
{
	const string PASSWORD = "password";
	const string USERNAME = "username";
	static readonly NetworkCredential _blankCreds = new();

	public NetworkCredential Credentials { get; }
	public bool IsCredentialsBlank => string.IsNullOrWhiteSpace(this.Credentials.UserName)
									  ||
									  string.IsNullOrWhiteSpace(this.Credentials.Password);
	public override bool IsTest => false;
	public override bool CanUseCookieAuthentication => true;

	public AuthedRequestMessage(HttpMethod method, string requestUri, NetworkCredential? credentials, IServiceScopeFactory scopeFactory)
		: base(method, requestUri, scopeFactory)
	{
		this.Credentials = credentials ?? _blankCreds;
	}

	/// <exception cref="UnauthorizedAccessException"></exception>
	public HttpContent GetLoginFormContent()
	{
		if (this.IsCredentialsBlank)
		{
			throw new UnauthorizedAccessException("The username or password is incorrect.");
		}

		return new FormUrlEncodedContent(GetFormContents(this.Credentials));
	}

	private static IEnumerable<KeyValuePair<string, string>> GetFormContents(NetworkCredential credential)
	{
		yield return new KeyValuePair<string, string>(USERNAME, credential.UserName);
		yield return new KeyValuePair<string, string>(PASSWORD, credential.Password);
	}
}
