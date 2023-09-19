using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Json;
using System.Buffers;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Services.Http
{
    public sealed class PathHandler : DelegatingHandler
    {
        const string API = "/api/";
        readonly JsonSerializerOptions _options;
        bool NoApiInPath { get; }

        public PathHandler(IConnectionSettings settings, SonarrJsonOptions options)
            : base()
        {
            _options = options.GetForSerializing();
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
            var respTask = base.SendAsync(request, cancellationToken);
            bool testing = request.Options.TryGetValue(SonarrClientDependencyInjection.KEY, out bool isTest)
                           &&
                           isTest;

            var response = await respTask.ConfigureAwait(false);
            HttpStatusCode originalCode = response.StatusCode;
            string? reason = response.ReasonPhrase;
            if (response.IsSuccessStatusCode && testing)
            {
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                bool isHtml = IsHtml(stream);
                await stream.DisposeAsync();

                if (isHtml)
                {
                    response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    {
                        Content = JsonContent.Create(new
                        {
                            Message = "The response returned something that looks like a HTML page. You sure the URL is correct?"
                        }, options: _options),
                        RequestMessage = request,
                        ReasonPhrase = HttpStatusCode.ServiceUnavailable.ToString(),
                    };
                }
                else
                {
                    response = new HttpResponseMessage(originalCode)
                    {
                        Content = new StringContent(string.Empty),
                        RequestMessage = request,
                        ReasonPhrase = reason,
                    };
                } 
            }

            return response;
        }

        private static bool IsHtml(Stream stream)
        {
            ReadOnlySpan<byte> badHtml = "<!doctype"u8;
            Span<byte> span = stackalloc byte[badHtml.Length];
            try
            {
                stream.ReadExactly(span);
                return badHtml.SequenceEqual(span);
            }
            catch
            {
                return false;
            }
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
