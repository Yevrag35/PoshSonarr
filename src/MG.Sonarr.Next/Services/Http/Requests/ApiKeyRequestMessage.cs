using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Services.Http.Requests;

/// <summary>
/// Represents a call to Sonarr's API that authenticates with the connection's API key.
/// </summary>
/// <remarks>
/// The request itself carries no key; the client that sends it adds the key's header to every request.
/// </remarks>
internal sealed class ApiKeyRequestMessage : SonarrRequestMessage
{
	/// <summary>
	/// Gets a value that indicates whether the request tests the connection to the server.
	/// </summary>
	/// <value>Always <see langword="false"/>.</value>
	public override bool IsTest => false;
	/// <summary>
	/// Gets a value that indicates whether the request can authenticate with a login cookie.
	/// </summary>
	/// <value>Always <see langword="false"/>; the request authenticates with the API key.</value>
	public override bool CanUseCookieAuthentication => false;

	/// <summary>
	/// Initializes a new <see cref="ApiKeyRequestMessage"/> instance with the specified HTTP method, URL string, and
	/// scope factory.
	/// </summary>
	/// <param name="method">The request's HTTP method.</param>
	/// <param name="requestUri">The endpoint's path, relative to Sonarr's API, such as <c>/series</c>.</param>
	/// <param name="scopeFactory">The factory that the request opens its scope from.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="method"/> is null.</exception>
	/// <exception cref="UriFormatException">Thrown when <paramref name="requestUri"/> isn't a valid relative or absolute URL.</exception>
	public ApiKeyRequestMessage(HttpMethod method, string requestUri, IServiceScopeFactory scopeFactory)
		: base(method, requestUri, scopeFactory)
	{
	}
	/// <summary>
	/// Initializes a new <see cref="ApiKeyRequestMessage"/> instance with the specified HTTP method, URL, and scope
	/// factory.
	/// </summary>
	/// <param name="method">The request's HTTP method.</param>
	/// <param name="requestUri">The endpoint's URL, relative to Sonarr's API or absolute.</param>
	/// <param name="scopeFactory">The factory that the request opens its scope from.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="method"/> is null.</exception>
	public ApiKeyRequestMessage(HttpMethod method, Uri requestUri, IServiceScopeFactory scopeFactory)
		: base(method, requestUri, scopeFactory)
	{
	}
}
