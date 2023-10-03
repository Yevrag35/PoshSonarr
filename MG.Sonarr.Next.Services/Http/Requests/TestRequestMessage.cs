namespace MG.Sonarr.Next.Services.Http.Requests
{
    internal sealed class TestRequestMessage : SonarrRequestMessage
    {
        public override bool IsTest => true;
        public override bool UseCookieAuthentication => false;
        public TestRequestMessage(string requestUri)
            : base(HttpMethod.Get, requestUri)
        {
        }
    }
}
