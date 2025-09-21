using System.Net.Http.Json;
using System.Net;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Services.Http.Requests;
using MG.Sonarr.Next.Services.Http.IO;
using MG.Sonarr.Next.Services.Http.Extensions;
using System.Text;

namespace MG.Sonarr.Next.Services.Http.Handlers
{
    public sealed class TestingHandler : DelegatingHandler
    {
        readonly JsonSerializerOptions _options;

        public TestingHandler(ISonarrJsonOptions options)
        {
            _options = options.ForSerializing;
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
                    request, response, cancellationToken).ConfigureAwait(false);
            }

            return response;
        }

        private async Task<HttpResponseMessage> ReadAndReturnNewResponse(HttpRequestMessage request, HttpResponseMessage response, CancellationToken cancellationToken)
        {
            bool isHtml = false;
            ArrayPoolMemoryStream memStream = new();

            Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false))
            {
                await stream.CopyToAsync(memStream, cancellationToken).ConfigureAwait(false);
                isHtml = IsHtml(memStream);
            }

            return isHtml
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = JsonContent.Create(new
                    {
                        Message = "The response returned something that looks like an HTML page. You sure the URL is correct?"
                    }, options: _options),
                    RequestMessage = request,
                    ReasonPhrase = nameof(HttpStatusCode.ServiceUnavailable),
                }
                : await ResetResponseAsync(response, memStream, cancellationToken).ConfigureAwait(false);
        }

        private static bool IsTesting(HttpRequestMessage request)
        {
            return request is SonarrRequestMessage sonarrRequest && sonarrRequest.IsTest;
        }
        private static bool IsHtml(ArrayPoolMemoryStream stream)
        {
            const string docType = "<!doctype";
            const string htmlType = "<html";
            const string xmlType = "<?xml";
            if (stream.Length < xmlType.Length)
            {
                return true;
            }

            Span<char> buffer = stackalloc char[docType.Length];
            ReadOnlySpan<byte> slice = stream.AsSpan(0, htmlType.Length);
            int written = Encoding.UTF8.GetChars(slice, buffer);
            if (buffer.Slice(0, written).Equals(htmlType, StringComparison.OrdinalIgnoreCase)
                ||
                buffer.Slice(0, written).Equals(xmlType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (stream.Length < docType.Length)
                return false;

            slice = stream.AsSpan(0, docType.Length);
            written = Encoding.UTF8.GetChars(slice, buffer);
            return buffer.Slice(0, written).Equals(docType, StringComparison.OrdinalIgnoreCase);
        }
        private static async Task<HttpResponseMessage> ResetResponseAsync(HttpResponseMessage original, ArrayPoolMemoryStream stream, CancellationToken token)
        {
            original.Content = await ResetStreamAsync(stream, token).ConfigureAwait(false);
            return original;
        }
        private static async Task<HttpContent> ResetStreamAsync(ArrayPoolMemoryStream stream, CancellationToken token)
        {
            stream.Rewind();
            return new StreamContent(await stream.ToMemoryStreamAsync(token).ConfigureAwait(false));
        }
    }
}
