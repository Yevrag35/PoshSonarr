﻿using System.Net.Http.Json;
using System.Net;
using MG.Sonarr.Next.Services.Json;

namespace MG.Sonarr.Next.Services.Http
{
    public sealed class TestingHandler : DelegatingHandler
    {
        JsonSerializerOptions Options { get; }

        public TestingHandler(SonarrJsonOptions options)
        {
            this.Options = options.GetForSerializing();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var respTask = base.SendAsync(request, cancellationToken);
            if (!IsTesting(request))
            {
                return await respTask.ConfigureAwait(false);
            }

            var response = await respTask.ConfigureAwait(false);

            HttpStatusCode originalCode = response.StatusCode;
            string? reason = response.ReasonPhrase;

            if (response.IsSuccessStatusCode)
            {
                response = await this.ReadAndReturnNewResponse(
                    request, response, originalCode, reason, cancellationToken);
            }

            return response;
        }

        private async Task<HttpResponseMessage> ReadAndReturnNewResponse(HttpRequestMessage request, HttpResponseMessage response, HttpStatusCode originalCode, string? reason, CancellationToken cancellationToken)
        {
            bool isHtml = false;
            await using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
            {
                isHtml = IsHtml(stream);
            }

            response = isHtml
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = JsonContent.Create(new
                    {
                        Message = "The response returned something that looks like an HTML page. You sure the URL is correct?"
                    }, options: this.Options),
                    RequestMessage = request,
                    ReasonPhrase = HttpStatusCode.ServiceUnavailable.ToString(),
                }
                : new HttpResponseMessage(originalCode)
                {
                    Content = new StringContent(string.Empty),
                    RequestMessage = request,
                    ReasonPhrase = reason,
                };

            return response;
        }

        private static bool IsTesting(HttpRequestMessage request)
        {
            return request.Options.TryGetValue(SonarrClientDependencyInjection.KEY, out bool isTest)
                   &&
                   isTest;
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
    }
}