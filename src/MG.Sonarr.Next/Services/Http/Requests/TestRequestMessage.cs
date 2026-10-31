using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Services.Http.Requests;

/// <summary>
/// Represents the <c>GET</c> request that checks a connection's URL and API key against a Sonarr server.
/// </summary>
/// <remarks>
/// The handlers check that a successful response to the request is a JSON object, and turn any other body into an
/// error that says the URL doesn't point at Sonarr's API.
/// </remarks>
internal sealed class TestRequestMessage : SonarrRequestMessage
{
	/// <summary>
	/// Gets a value that indicates whether the request tests the connection to the server.
	/// </summary>
	/// <value>Always <see langword="true"/>.</value>
	public override bool IsTest => true;
	/// <summary>
	/// Gets a value that indicates whether the request can authenticate with a login cookie.
	/// </summary>
	/// <value>Always <see langword="false"/>; the request authenticates with the API key.</value>
	public override bool CanUseCookieAuthentication => false;
	/// <summary>
	/// Initializes a new <see cref="TestRequestMessage"/> instance with the specified URL and scope factory.
	/// </summary>
	/// <param name="requestUri">The path of the endpoint to test, relative to Sonarr's API, such as <c>/system/status</c>.</param>
	/// <param name="scopeFactory">The factory that the request opens its scope from.</param>
	/// <exception cref="UriFormatException">Thrown when <paramref name="requestUri"/> isn't a valid relative or absolute URL.</exception>
	public TestRequestMessage(string requestUri, IServiceScopeFactory scopeFactory)
		: base(HttpMethod.Get, requestUri, scopeFactory)
	{
	}
}
