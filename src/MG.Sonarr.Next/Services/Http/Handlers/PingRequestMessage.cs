using MG.Sonarr.Next.Services.Http.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Services.Http.Handlers
{
    internal sealed class PingRequestMessage : SonarrRequestMessage
    {
        public PingRequestMessage(string requestUri, IServiceScopeFactory scopeFactory)
            : base(HttpMethod.Get, requestUri, scopeFactory)
        {
        }

        public override bool IsTest => true;
        public override bool CanUseCookieAuthentication => false;
    }
}
