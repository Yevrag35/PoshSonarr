using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Services.Http.Requests
{
    internal sealed class ApiKeyRequestMessage : SonarrRequestMessage
    {
        public override bool IsTest => false;
        public override bool CanUseCookieAuthentication => false;

        public ApiKeyRequestMessage(HttpMethod method, string requestUri, IServiceScopeFactory scopeFactory)
            : base(method, requestUri, scopeFactory)
        {
        }
        public ApiKeyRequestMessage(HttpMethod method, Uri requestUri, IServiceScopeFactory scopeFactory)
            : base(method, requestUri, scopeFactory)
        {
        }
    }
}
