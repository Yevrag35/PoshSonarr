using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Services.Http.Requests;

namespace MG.Sonarr.Next.Services.Http.Handlers
{
    public sealed class PathHandler : DelegatingHandler
    {
        const string API = "/api";
        const string V3 = "/v3/";
        readonly bool _noApiInPath;

        public PathHandler(IConnectionSettings settings)
        {
            _noApiInPath = settings.NoApiInPath;
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request is not AuthedRequestMessage)
            {
                this.SetPath(request);
            }

            return base.Send(request, cancellationToken);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            if (request is not AuthedRequestMessage)
            {
                this.SetPath(request);
            }

            return base.SendAsync(request, cancellationToken);
        }

        private void SetPath(HttpRequestMessage request)
        {
            ReadOnlySpan<char> path = request.RequestUri?.GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped);
            if (path.StartsWith('/'))
            {
                path = path.TrimStart('/');
            }

            if (path.StartsWith("api/v3", StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            ReadOnlySpan<char> authority = request.RequestUri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);


            Span<char> span = stackalloc char[path.Length + API.Length + V3.Length + authority.Length];
            int position = 0;
            authority.CopyToSlice(span, ref position);

            if (!_noApiInPath)
            {
                API.CopyToSlice(span, ref position);
            }

            V3.CopyToSlice(span, ref position);
            if (path.StartsWith('/', StringComparison.InvariantCulture))
            {
                position--;
            }

            path.CopyToSlice(span, ref position);

            request.RequestUri = new Uri(new string(span.Slice(0, position)), UriKind.Absolute);
        }
    }
}
