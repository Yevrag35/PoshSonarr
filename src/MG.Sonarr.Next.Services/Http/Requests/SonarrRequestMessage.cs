namespace MG.Sonarr.Next.Services.Http.Requests
{
    public abstract class SonarrRequestMessage : HttpRequestMessage, IHttpRequestDetails
    {
        public abstract bool IsTest { get; }
        string IHttpRequestDetails.Method => this.Method.Method;
        public string OriginalRequestUri { get; }
        string IHttpRequestDetails.RequestUri => this.GetRequestUri();
        public abstract bool UseCookieAuthentication { get; }

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

        protected virtual string GetRequestUri()
        {
            return this.RequestUri?.ToString() ?? this.OriginalRequestUri;
        }
    }
}
