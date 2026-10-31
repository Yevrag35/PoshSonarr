using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Services.Http.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Services.Http.Handlers;

/// <summary>
/// Provides a delegating handler that writes a request and its response to the verbose output of the cmdlet that sent
/// the request.
/// </summary>
/// <remarks>
/// <para>The handler finds the sending <see cref="IApiCmdlet"/> in the request's options, where the client that sends
/// the request puts it. When there's no cmdlet, or its <see cref="IApiCmdlet.CanWriteVerbose"/> is
/// <see langword="false"/>, the request passes through unchanged.</para>
/// <para>The handler calls <see cref="IApiCmdlet.WriteVerboseAfter(ISonarrResponse, IServiceProvider, JsonSerializerOptions?)"/>
/// after an <see langword="await"/>, often on a thread-pool thread, so the cmdlet has to write its output in a way
/// that works off the pipeline thread.</para>
/// <para>The handler keeps no per-request state, so one instance can serve concurrent requests.</para>
/// </remarks>
public sealed class VerboseHandler : DelegatingHandler
{
	readonly JsonSerializerOptions _options;
	readonly IServiceScopeFactory _scopeFactory;

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
	/// Sends the request to the inner handler and blocks until its response arrives, writing both to the sending cmdlet's
	/// verbose output.
	/// </summary>
	/// <remarks>
	/// The method blocks on <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>, so the inner handlers run on
	/// their asynchronous path.
	/// </remarks>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="cancellationToken">A token that cancels the operation.</param>
	/// <returns>The HTTP response message from the inner handler.</returns>
	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return this.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
	}
	/// <summary>
	/// Sends the request to the inner handler asynchronously, writing it and its response to the sending cmdlet's verbose
	/// output when that output is enabled.
	/// </summary>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="cancellationToken">A token that cancels the operation.</param>
	/// <returns>A task that completes with the HTTP response message from the inner handler.</returns>
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return ApiCmdletAccessor.TryGetCmdlet(request, out IApiCmdlet? cmdlet) && cmdlet.CanWriteVerbose
			? this.SendForCmdletAsync(request, cmdlet, cancellationToken)
			: base.SendAsync(request, cancellationToken);
	}

	/// <summary>
	/// Writes the request to the cmdlet's verbose output, sends it to the inner handler, and writes the response's status
	/// code and URL afterward.
	/// </summary>
	/// <remarks>
	/// Only a <see cref="SonarrRequestMessage"/> is written before the send. When the inner handler throws, nothing is
	/// written after it.
	/// </remarks>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="cmdlet">The cmdlet that sent the request, whose verbose output is enabled.</param>
	/// <param name="cancellationToken">A token that cancels the operation.</param>
	/// <returns>A task that completes with the HTTP response message from the inner handler.</returns>
	private async Task<HttpResponseMessage> SendForCmdletAsync(HttpRequestMessage request, IApiCmdlet cmdlet, CancellationToken cancellationToken)
	{
		if (request is SonarrRequestMessage sr)
		{
			cmdlet.WriteVerboseBefore(sr);
		}

		var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

		using var scope = _scopeFactory.CreateScope();
		cmdlet.WriteVerboseAfter(
			response: new SonarrClientResult(response, request.RequestUri?.ToString()),
			provider: scope.ServiceProvider,
			options: _options);

		return response;
	}
}
