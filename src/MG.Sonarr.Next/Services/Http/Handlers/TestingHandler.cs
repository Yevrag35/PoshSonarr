using System.Net.Http.Json;
using System.Net;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Services.Http.Requests;

namespace MG.Sonarr.Next.Services.Http.Handlers
{
    public sealed class TestingHandler : DelegatingHandler
    {
        readonly JsonSerializerOptions _options;

        public TestingHandler(ISonarrJsonOptions options)
        {
            _options = options.GetForSerializing();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var respTask = base.SendAsync(request, cancellationToken);
            if (!IsTesting(request))
            {
                return await respTask.ConfigureAwait(false);
            }

            var response = await respTask.ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                response = await this.ReadAndReturnNewResponse(
                    request, response, cancellationToken);
            }

            return response;
        }

        private async Task<HttpResponseMessage> ReadAndReturnNewResponse(HttpRequestMessage request, HttpResponseMessage response, CancellationToken cancellationToken)
        {
            bool isHtml = false;
            MemoryStream memStream = new MemoryStream();

            await using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
            {
                await stream.CopyToAsync(memStream, cancellationToken);
                isHtml = IsHtml(memStream);
            }

            response = isHtml
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = JsonContent.Create(new
                    {
                        Message = "The response returned something that looks like an HTML page. You sure the URL is correct?"
                    }, options: _options),
                    RequestMessage = request,
                    ReasonPhrase = HttpStatusCode.ServiceUnavailable.ToString(),
                }
                : ResetResponse(response, memStream);

            return response;
        }

        private static bool IsTesting(HttpRequestMessage request)
        {
            return request is SonarrRequestMessage sonarrRequest && sonarrRequest.IsTest;
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
        private static HttpResponseMessage ResetResponse(HttpResponseMessage original, MemoryStream stream)
        {
            original.Content = ResetStream(stream);
            return original;
        }
        private static HttpContent ResetStream(MemoryStream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return new StreamContent(stream);
        }
    }
}
