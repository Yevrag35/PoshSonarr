namespace MG.Sonarr.Next.Services.Http.Requests
{
    public abstract class SonarrRequestMessage : HttpRequestMessage, IHttpRequestDetails
    {
        public abstract bool IsTest { get; }
        public string RequestMethod => this.Method.Method;
        public string OriginalRequestUri { get; }
        public string RequestUrl => this.RequestUri?.ToString() ?? this.OriginalRequestUri;
        public abstract bool CanUseCookieAuthentication { get; }

        public SonarrRequestMessage(HttpMethod method, string requestUri)
            : base(method, requestUri)
        {
            this.OriginalRequestUri = requestUri ?? string.Empty;
        }
        public SonarrRequestMessage(HttpMethod method, Uri requestUri)
            : base(method, requestUri)
        {
            this.OriginalRequestUri = requestUri.OriginalString;
        }
    }
}
