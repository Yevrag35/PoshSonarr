using MG.Sonarr.Next.Services.Json;
using OneOf.Types;
using System.Management.Automation;

namespace MG.Sonarr.Next.Services.Http
{
    public sealed class VerboseHandler : DelegatingHandler
    {
        readonly Queue<IApiCmdlet> _queue;
        readonly JsonSerializerOptions _options;

        public VerboseHandler(Queue<IApiCmdlet> queue, SonarrJsonOptions options)
        {
            _queue = queue;
            _options = options.GetForSerializing();
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_queue.TryDequeue(out IApiCmdlet? cmdlet))
            {
                cmdlet.WriteVerbose(request);
            }

            var response = base.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
            cmdlet?.WriteVerboseSonarrResult(
                SonarrResponse.Create(response, request.RequestUri?.ToString() ?? string.Empty),
                _options);

            return response;
        }
    }
}
