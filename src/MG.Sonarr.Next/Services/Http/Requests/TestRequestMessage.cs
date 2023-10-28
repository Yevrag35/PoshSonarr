using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Services.Http.Requests
{
    internal sealed class TestRequestMessage : SonarrRequestMessage
    {
        public override bool IsTest => true;
        public override bool CanUseCookieAuthentication => false;
        public TestRequestMessage(string requestUri, IServiceScopeFactory scopeFactory)
            : base(HttpMethod.Get, requestUri, scopeFactory)
        {
        }
    }
}
