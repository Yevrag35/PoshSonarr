using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Services.Http.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Services.Http.Handlers;

public sealed class VerboseHandler : DelegatingHandler
{
	readonly JsonSerializerOptions _options;
	readonly IServiceScopeFactory _scopeFactory;

	public VerboseHandler(ISonarrJsonOptions options, IServiceScopeFactory scopeFactory)
	{
		_options = options.ForSerializing;
		_scopeFactory = scopeFactory;
	}

	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return this.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
	}
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return ApiCmdletAccessor.TryGetCmdlet(request, out IApiCmdlet? cmdlet) && cmdlet.CanWriteVerbose
			? this.SendForCmdletAsync(request, cmdlet, cancellationToken)
			: base.SendAsync(request, cancellationToken);
	}

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
