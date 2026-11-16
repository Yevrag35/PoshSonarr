namespace MG.Sonarr.Next.Services.Http.Handlers;

/// <summary>
/// Provides a base class for delegating handlers that send requests only synchronously.
/// </summary>
/// <remarks>
/// <para>The module's clients send every request synchronously, so a derived handler overrides only
/// <see cref="DelegatingHandler.Send(HttpRequestMessage, CancellationToken)"/> and passes the request on with
/// <c>base.Send</c>.</para>
/// <para>A handler that overrides only <c>Send</c> inherits
/// <see cref="DelegatingHandler.SendAsync(HttpRequestMessage, CancellationToken)"/>, which passes a request to the
/// inner handler without running the handler's own code and without an error. A request sent asynchronously would
/// then reach the server without any of the handlers' work, such as the URL base and the API prefix in its URL or a
/// download's credentials. This class seals <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/> to fail
/// such a request instead.</para>
/// <para>The class leaves <c>Send</c> as <see cref="DelegatingHandler"/> implements it rather than making it abstract,
/// since a derived handler passes a request on with <c>base.Send</c>, which C# doesn't allow when the base member is
/// abstract. A derived handler that doesn't override <c>Send</c> still compiles, and passes every request through
/// untouched.</para>
/// </remarks>
internal abstract class SynchronousHandler : DelegatingHandler
{
	/// <summary>
	/// Throws a <see cref="NotSupportedException"/>, since the handler sends requests only synchronously.
	/// </summary>
	/// <param name="request">The HTTP request message, which the method doesn't send.</param>
	/// <param name="cancellationToken">The cancellation token, which the method ignores.</param>
	/// <returns>No task, since the method always throws.</returns>
	/// <exception cref="NotSupportedException">Thrown when the method is called, since the handler sends requests only
	/// synchronously.</exception>
	protected sealed override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		throw new NotSupportedException($"{this.GetType().Name} sends requests only synchronously, through HttpClient.Send.");
	}
}
