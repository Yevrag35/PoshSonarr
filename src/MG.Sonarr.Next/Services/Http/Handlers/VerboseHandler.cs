using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Services.Http.Requests;

namespace MG.Sonarr.Next.Services.Http.Handlers;

/// <summary>
/// Provides a delegating handler that writes a request and its response to the verbose output of the cmdlet that sent
/// the request.
/// </summary>
/// <remarks>
/// <para>The handler finds the sending <see cref="IApiCmdlet"/> in the request's options, where the client that sends
/// the request puts it. When there's no cmdlet, or its <see cref="IApiCmdlet.CanWriteVerbose"/> is
/// <see langword="false"/>, the request passes through unchanged.</para>
/// <para>The handler overrides only <see cref="Send(HttpRequestMessage, CancellationToken)"/>, since the clients send
/// every request synchronously, so it calls the cmdlet's write methods on the thread that sent the request, which for a
/// cmdlet is its pipeline thread. A request that reaches it through
/// <see cref="SynchronousHandler.SendAsync(HttpRequestMessage, CancellationToken)"/> instead fails with a
/// <see cref="NotSupportedException"/>.</para>
/// <para>The handler keeps no per-request state, so one instance can serve concurrent requests.</para>
/// </remarks>
internal sealed class VerboseHandler : SynchronousHandler
{
	private readonly JsonSerializerOptions _options;
	private readonly IServiceScopeFactory _scopeFactory;

	/// <summary>
	/// Initializes a new <see cref="VerboseHandler"/> instance with the specified JSON options and scope factory.
	/// </summary>
	/// <param name="options">The JSON options whose <see cref="ISonarrJsonOptions.ForSerializing"/> options the cmdlet
	/// receives to format the response.</param>
	/// <param name="scopeFactory">The factory that creates a service scope for each response that the handler
	/// writes.</param>
	public VerboseHandler(ISonarrJsonOptions options, IServiceScopeFactory scopeFactory)
	{
		_options = options.ForSerializing;
		_scopeFactory = scopeFactory;
	}

	/// <summary>
	/// Sends the request to the inner handler, writing it and its response to the sending cmdlet's verbose output when
	/// that output is enabled.
	/// </summary>
	/// <remarks>
	/// Only a request that implements <see cref="IHttpRequestDetails"/>, as every <see cref="SonarrRequestMessage"/>
	/// does, is written before the send. When the inner handler throws, nothing is written after it. When the write
	/// after it throws, as a cmdlet's own verbose output does under a preference of
	/// <see cref="ActionPreference.Stop"/>, the method disposes the response before the exception leaves it.
	/// </remarks>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="cancellationToken">A token that cancels the operation.</param>
	/// <returns>The HTTP response message from the inner handler.</returns>
	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (!ApiCmdletAccessor.TryGetCmdlet(request, out IApiCmdlet? cmdlet) || !cmdlet.CanWriteVerbose)
		{
			return base.Send(request, cancellationToken);
		}

		if (request is IHttpRequestDetails sr)
		{
			cmdlet.WriteVerboseBefore(sr);
		}

		HttpResponseMessage response = base.Send(request, cancellationToken);
		try
		{
			using IServiceScope scope = _scopeFactory.CreateScope();
			cmdlet.WriteVerboseAfter(
				response: new SonarrClientResult(response, request.RequestUri?.ToString()),
				provider: scope.ServiceProvider,
				options: _options);
		}
		catch
		{
			response.Dispose();
			throw;
		}

		return response;
	}
}
