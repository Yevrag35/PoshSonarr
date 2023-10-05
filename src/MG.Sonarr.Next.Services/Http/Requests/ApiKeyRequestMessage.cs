namespace MG.Sonarr.Next.Services.Http.Requests
{
    internal sealed class ApiKeyRequestMessage : SonarrRequestMessage
    {
        public override bool IsTest => false;
        public override bool UseCookieAuthentication => false;

        public ApiKeyRequestMessage(HttpMethod method, string requestUri)
            : base(method, requestUri)
        {
        }
        public ApiKeyRequestMessage(HttpMethod method, Uri requestUri)
            : base(method, requestUri)
        {
        }
    }
}
