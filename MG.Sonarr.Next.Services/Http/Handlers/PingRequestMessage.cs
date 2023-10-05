using MG.Sonarr.Next.Services.Http.Requests;

namespace MG.Sonarr.Next.Services.Http.Handlers
{
    internal sealed class PingRequestMessage : SonarrRequestMessage
    {
        public PingRequestMessage(string requestUri)
            : base(HttpMethod.Get, requestUri)
        {
        }

        public override bool IsTest => true;
        public override bool UseCookieAuthentication => false;
    }
}
