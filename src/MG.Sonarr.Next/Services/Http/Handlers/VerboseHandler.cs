using MG.Sonarr.Next.Services.Http.Requests;
using MG.Sonarr.Next.Json;
using Microsoft.Extensions.DependencyInjection;
using MG.Sonarr.Next.Services.Jobs;
using System.Runtime.InteropServices;
using System.Reflection;

namespace MG.Sonarr.Next.Services.Http.Handlers
{
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
            //if (_queue.TryDequeue(out IApiCmdlet? cmdlet) && request is SonarrRequestMessage sr)
            //{
            //    cmdlet.WriteVerboseBefore(sr);
            //}

            //var response = this.SendAsync(request, cancellationToken).GetAwaiter().GetResult();

            //using (var scope = _scopeFactory.CreateScope())
            //{
            //    cmdlet?.WriteVerboseAfter(
            //        response: SonarrResponse.Create(response, request.RequestUri?.ToString() ?? string.Empty),
            //        provider: scope.ServiceProvider,
            //        options: _options);
            //}

            //return response;
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_queue.TryDequeue(out IApiCmdlet? cmdlet) && request is SonarrRequestMessage sr)
            {
                cmdlet.WriteVerboseBefore(sr);
            }

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            using (var scope = _scopeFactory.CreateScope())
            {
                cmdlet?.WriteVerboseAfter(
                    response: SonarrResponse.Create(response, request.RequestUri?.ToString() ?? string.Empty),
                    provider: scope.ServiceProvider,
                    options: _options);
            }

            return response;
        }
    }
}
