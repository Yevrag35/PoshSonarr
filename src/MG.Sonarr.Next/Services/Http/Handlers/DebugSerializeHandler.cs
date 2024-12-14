using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Services.Jobs;
using MG.Sonarr.Next.Strings;
using MG.Sonarr.Resources;
using System.Runtime.InteropServices;
using System.Text;

namespace MG.Sonarr.Next.Services.Http.Handlers;

public sealed class DebugSerializeHandler : DelegatingHandler
{
    private readonly ApiCmdletQueue _queue;
    private readonly JsonSerializerOptions _serializingOptions;
    
    public DebugSerializeHandler(ApiCmdletQueue queue, ISonarrJsonOptions options)
    {
        _queue = queue;
        _serializingOptions = options.ForDebugging;
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return this.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!_queue.TryPeek(out IApiCmdlet? cmdlet))
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        //if (cmdlet.CanDebugSerializeBefore && request.Content is not null)
        //{
        //    string jsonString = await this.SerializeRequest(request, request.Content, cancellationToken).ConfigureAwait(false);
        //    cmdlet.WriteDebugPayload(jsonString);
        //}

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (cmdlet.CanDebugSerializeAfter && response.Content is not null)
        {
            StringResponse parsed = await this.SerializeResponse(response, response.Content, cancellationToken).ConfigureAwait(false);
            string jsonString = Messenger.Format(
                provider: CultureInfo.CurrentCulture,
                format: Messages.Debug_JSONResponse_Preamble,
                [parsed.ContentLength, parsed.JsonString]);

            cmdlet.WriteDebugPayload(jsonString);
        }

        return response;
    }

    private async Task<StringResponse> SerializeResponse(HttpResponseMessage response, HttpContent content, CancellationToken token)
    {
        byte[] responseBytes = await content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
        string jsonString = Encoding.UTF8.GetString(responseBytes);

        try
        {
            response.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            CopyHeaders(content, response.Content);
            return new(responseBytes.Length, jsonString);
        }
        finally
        {
            content.Dispose();
        }
    }

    private static void CopyHeaders(HttpContent original, HttpContent copy)
    {
        foreach (var kvp in original.Headers)
        {
            copy.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
        }
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct StringResponse
    {
        public readonly int ContentLength;
        public readonly string JsonString;

        public StringResponse(int contentLength, string jsonString)
        {
            ContentLength = contentLength;
            JsonString = jsonString;
        }
    }
}
