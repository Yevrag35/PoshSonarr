using MG.Sonarr.Next.Services.Json;
using Microsoft.Extensions.DependencyInjection;
using OneOf.Types;
using System.Management.Automation;

namespace MG.Sonarr.Next.Services.Http
{
    public sealed class VerboseHandler : DelegatingHandler
    {
        readonly Queue<IApiCmdlet> _queue;
        readonly JsonSerializerOptions _options;
        readonly IServiceScopeFactory _scopeFactory;

        public VerboseHandler(Queue<IApiCmdlet> queue, SonarrJsonOptions options, IServiceScopeFactory scopeFactory)
        {
            _queue = queue;
            _options = options.GetForSerializing();
            _scopeFactory = scopeFactory;
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_queue.TryDequeue(out IApiCmdlet? cmdlet))
            {
                cmdlet.WriteVerbose(request);
            }

            var response = base.SendAsync(request, cancellationToken).GetAwaiter().GetResult();

            using (var scope = _scopeFactory.CreateScope())
            {
                cmdlet?.WriteVerboseSonarrResult(
                    response: SonarrResponse.Create(response, request.RequestUri?.ToString() ?? string.Empty),
                    provider: scope.ServiceProvider,
                    options: _options);
            }

            return response;
        }
    }
}
