using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Services.Http.Requests;

/// <summary>
/// Represents an HTTP request that the module sends to a Sonarr server, which keeps the path its caller gave and
/// provides services from a dependency injection scope of its own.
/// </summary>
/// <remarks>
/// <para>
///     Each instance opens a new scope from the factory it's given and disposes that scope when the instance is
///     disposed. The request, with its content and its scope, has to stay alive until its response has been read.
/// </para>
/// <para>
///     The handlers that send the request can rewrite <see cref="HttpRequestMessage.RequestUri"/>, so
///     <see cref="OriginalRequestUri"/> keeps the URL as the caller passed it.
/// </para>
/// </remarks>
public abstract class SonarrRequestMessage : HttpRequestMessage, IHttpRequestDetails
{
	bool _disposed;
	readonly IServiceScope _scope;

	/// <summary>
	/// When overridden in a derived class, gets a value that indicates whether the request tests the connection to
	/// the server.
	/// </summary>
	/// <remarks>
	/// A test request asks for Sonarr's system status, and the handlers check that its response came from Sonarr's API.
	/// </remarks>
	/// <value>
	///     <see langword="true"/> if the request tests the URL and API key of a connection; otherwise,
	///     <see langword="false"/>.
	/// </value>
	public abstract bool IsTest { get; }
	/// <summary>
	/// Gets the name of the request's HTTP method.
	/// </summary>
	/// <value>The method's name, such as <c>GET</c> or <c>POST</c>.</value>
	public string RequestMethod => this.Method.Method;
	/// <summary>
	/// Gets the URL that the request was created with.
	/// </summary>
	/// <value>
	///     The URL as the caller passed it, before any handler changed it, or an empty string when the request was
	///     created without one.
	/// </value>
	public string OriginalRequestUri { get; }
	/// <summary>
	/// Gets the URL that the request is sent to.
	/// </summary>
	/// <value>
	///     The current value of <see cref="HttpRequestMessage.RequestUri"/> as a string, or
	///     <see cref="OriginalRequestUri"/> when it's <see langword="null"/>.
	/// </value>
	public string RequestUrl => this.RequestUri?.ToString() ?? this.OriginalRequestUri;
	/// <summary>
	/// When overridden in a derived class, gets a value that indicates whether the request can authenticate with a
	/// login cookie.
	/// </summary>
	/// <value>
	///     <see langword="true"/> if the request can carry the cookie from a forms login; otherwise,
	///     <see langword="false"/>.
	/// </value>
	public abstract bool CanUseCookieAuthentication { get; }

	/// <summary>
	/// Initializes a new <see cref="SonarrRequestMessage"/> instance with the specified HTTP method, URL string, and
	/// scope factory.
	/// </summary>
	/// <param name="method">The request's HTTP method.</param>
	/// <param name="requestUri">
	///     The request's URL, usually a path relative to Sonarr's API, such as <c>/series</c>. It can be
	///     <see langword="null"/>.
	/// </param>
	/// <param name="scopeFactory">The factory that the request opens its scope from. This value must not be <see langword="null"/>.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="method"/> is null.</exception>
	/// <exception cref="UriFormatException">Thrown when <paramref name="requestUri"/> isn't a valid relative or absolute URL.</exception>
	protected SonarrRequestMessage(HttpMethod method, string requestUri, IServiceScopeFactory scopeFactory)
		: base(method, requestUri)
	{
		this.OriginalRequestUri = requestUri ?? string.Empty;
		_scope = scopeFactory.CreateScope();
	}
	/// <summary>
	/// Initializes a new <see cref="SonarrRequestMessage"/> instance with the specified HTTP method, URL, and scope
	/// factory.
	/// </summary>
	/// <param name="method">The request's HTTP method.</param>
	/// <param name="requestUri">The request's URL, relative or absolute. This value must not be <see langword="null"/>.</param>
	/// <param name="scopeFactory">The factory that the request opens its scope from. This value must not be <see langword="null"/>.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="method"/> is null.</exception>
	protected SonarrRequestMessage(HttpMethod method, Uri requestUri, IServiceScopeFactory scopeFactory)
		: base(method, requestUri)
	{
		this.OriginalRequestUri = requestUri.OriginalString;
		_scope = scopeFactory.CreateScope();
	}

	/// <summary>
	/// Gets the service of the specified type from the request's scope.
	/// </summary>
	/// <param name="serviceType">The type of the service to get.</param>
	/// <returns>The service, or <see langword="null"/> if the scope has no service of <paramref name="serviceType"/>.</returns>
	/// <exception cref="ObjectDisposedException">Thrown when the request has been disposed.</exception>
	public object? GetService(Type serviceType)
	{
		return _scope.ServiceProvider.GetService(serviceType);
	}

	/// <summary>
	/// Releases the request's scope, when called from <see cref="HttpRequestMessage.Dispose()"/>, and the resources
	/// that the base request holds.
	/// </summary>
	/// <remarks>
	/// The scope disposes the disposable services it created. Calling the method more than once disposes the scope
	/// only the first time.
	/// </remarks>
	/// <param name="disposing">
	///     <see langword="true"/> to release managed and unmanaged resources; <see langword="false"/> to release only
	///     unmanaged resources.
	/// </param>
	protected override void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing)
			{
				_scope.Dispose();
			}

			_disposed = true;
		}

		base.Dispose(disposing);
	}
}
