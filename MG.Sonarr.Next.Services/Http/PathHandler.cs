using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Json;
using System.Buffers;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Services.Http
{
    public sealed class PathHandler : DelegatingHandler
    {
        const string API = "/api";
        const string V3 = "/v3/";
        bool NoApiInPath { get; }

        public PathHandler(IConnectionSettings settings)
        {
            this.NoApiInPath = settings.NoApiInPath;
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.SetPath(request);
            return base.Send(request, cancellationToken);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            this.SetPath(request);
            return base.SendAsync(request, cancellationToken);
        }

        private void SetPath(HttpRequestMessage request)
        {
            ReadOnlySpan<char> path = request.RequestUri?.GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped);
            ReadOnlySpan<char> authority = request.RequestUri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);

            Span<char> span = stackalloc char[path.Length + API.Length + V3.Length + authority.Length];
            int position = 0;
            authority.CopyToSlice(span, ref position);

            if (!this.NoApiInPath)
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

            //if (request.RequestUri is not null && request.RequestUri.IsAbsoluteUri && !this.NoApiInPath)
            //{
            //    request.RequestUri = this.AddApiToPath(request.RequestUri);
            //}
        }
        //private Uri AddApiToPath(Uri requestUri)
        //{
        //    string path = requestUri.GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped);
        //    ReadOnlySpan<char> chars = path.AsSpan();

        //    if (chars.StartsWith(new ReadOnlySpan<char>('/'), StringComparison.InvariantCultureIgnoreCase))
        //    {
        //        chars = chars.Slice(1);
        //    }

        //    if (!chars.StartsWith(API.AsSpan(1), StringComparison.InvariantCultureIgnoreCase))
        //    {
        //        ReadOnlySpan<char> authority = requestUri.GetComponents(
        //            UriComponents.SchemeAndServer, UriFormat.Unescaped);

        //        Span<char> newPath = stackalloc char[chars.Length + API.Length + V3.Length + authority.Length];
                
        //        int position = 0;
        //        authority.CopyToSlice(newPath, ref position);
        //        API.CopyToSlice(newPath, ref position);
        //        chars.CopyToSlice(newPath, ref position);

        //        return new Uri(new string(newPath.Slice(0, position)), UriKind.Absolute);
        //    }
        //    else if (!chars.StartsWith(V3.AsSpan(1), StringComparison.InvariantCultureIgnoreCase))
        //    {
        //        return requestUri;
        //    } 
        //}
    }
}
