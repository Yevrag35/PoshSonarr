using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Services.Http.Requests;
using MG.Sonarr.Next.Services.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Services.Http.Handlers;

public sealed class VerboseHandler : DelegatingHandler
{
	readonly ApiCmdletQueue _queue;
	readonly JsonSerializerOptions _options;
	readonly IServiceScopeFactory _scopeFactory;

	public VerboseHandler(ApiCmdletQueue queue, ISonarrJsonOptions options, IServiceScopeFactory scopeFactory)
	{
		_queue = queue;
		_options = options.ForSerializing;
		_scopeFactory = scopeFactory;
	}

	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return this.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
	}
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (_queue.TryDequeue(out IApiCmdlet? cmdlet))
		{
			if (cmdlet.CanWriteVerbose && request is SonarrRequestMessage sr)
			{
				cmdlet.WriteVerboseBefore(sr);
			}
		}

		try
		{
			bool canWriteVerbose = false;
			if (cmdlet is not null)
			{
				canWriteVerbose = cmdlet.CanWriteVerbose;
				_queue.Enqueue(cmdlet);
			}

			var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

			if (canWriteVerbose)
			{
				using var scope = _scopeFactory.CreateScope();
				cmdlet?.WriteVerboseAfter(
					response: new SonarrClientResult(response, request.RequestUri?.ToString()),
					provider: scope.ServiceProvider,
					options: _options);
			}

			return response;
		}
		finally
		{
			if (cmdlet is not null)
			{
				_ = _queue.Dequeue();
			}
		}
	}
}
