using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Services.Http.Requests
{
    public abstract class SonarrRequestMessage : HttpRequestMessage, IHttpRequestDetails
    {
        bool _disposed;
        readonly IServiceScope _scope;

        public abstract bool IsTest { get; }
        public string RequestMethod => this.Method.Method;
        public string OriginalRequestUri { get; }
        public string RequestUrl => this.RequestUri?.ToString() ?? this.OriginalRequestUri;
        public abstract bool CanUseCookieAuthentication { get; }

        protected SonarrRequestMessage(HttpMethod method, string requestUri, IServiceScopeFactory scopeFactory)
            : base(method, requestUri)
        {
            this.OriginalRequestUri = requestUri ?? string.Empty;
            _scope = scopeFactory.CreateScope();
        }
        protected SonarrRequestMessage(HttpMethod method, Uri requestUri, IServiceScopeFactory scopeFactory)
            : base(method, requestUri)
        {
            this.OriginalRequestUri = requestUri.OriginalString;
            _scope = scopeFactory.CreateScope();
        }

        public object? GetService(Type serviceType)
        {
            return _scope.ServiceProvider.GetService(serviceType);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _scope.Dispose();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
