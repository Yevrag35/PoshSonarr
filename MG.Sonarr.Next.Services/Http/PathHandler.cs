using MG.Sonarr.Next.Services.Auth;

namespace MG.Sonarr.Next.Services.Http
{
    public sealed class PathHandler : DelegatingHandler
    {
        const string API = "/api/";
        bool NoApiInPath { get; }

        public PathHandler(IConnectionSettings settings)
            : base()
        {
            this.NoApiInPath = settings.NoApiInPath;
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = this.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
            return response;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.SetPath(request);

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            Debug.Assert(response.IsSuccessStatusCode);

            return response;
        }

        private void SetPath(HttpRequestMessage request)
        {
            if (request.RequestUri is not null && request.RequestUri.IsAbsoluteUri && !this.NoApiInPath)
            {
                request.RequestUri = this.AddApiToPath(request.RequestUri);
            }
        }
        private Uri AddApiToPath(Uri requestUri)
        {
            string path = requestUri.AbsolutePath;
            ReadOnlySpan<char> chars = path.AsSpan();

            if (chars.StartsWith(new ReadOnlySpan<char>('/'), StringComparison.InvariantCultureIgnoreCase))
            {
                chars = chars.Slice(1);
            }

            if (!chars.StartsWith(API.AsSpan(1), StringComparison.InvariantCultureIgnoreCase))
            {
                ReadOnlySpan<char> authority = requestUri.GetComponents(
                    UriComponents.SchemeAndServer, UriFormat.Unescaped);

                Span<char> newPath = stackalloc char[chars.Length + API.Length + authority.Length];
                authority.CopyTo(newPath);
                int position = authority.Length;
                API.CopyTo(newPath.Slice(position));
                position += API.Length;
                chars.CopyTo(newPath.Slice(position));
                position += chars.Length;

                return new Uri(new string(newPath.Slice(0, position)), UriKind.Absolute);
            }
            else
            {
                return requestUri;
            } 
        }
    }
}
