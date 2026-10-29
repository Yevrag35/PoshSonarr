using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Services.Http.Extensions;
using MG.Sonarr.Next.Services.Http.IO;
using MG.Sonarr.Next.Services.Http.Requests;
using System.Net;
using System.Net.Http.Json;

namespace MG.Sonarr.Next.Services.Http.Handlers;

public sealed class TestingHandler : DelegatingHandler
{
	const string NOT_API_MESSAGE = "The response isn't the JSON object that Sonarr's API returns, so it may be an HTML page. You sure the URL is correct?";

	readonly JsonSerializerOptions _options;

	public TestingHandler(ISonarrJsonOptions options)
	{
		_options = options.ForSerializing;
	}

	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return this.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
	}

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request.Content is not null)
		{
			// A buffered body goes out with a Content-Length header, where a JsonContent body would go out chunked.
			await request.Content.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
		}

		HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

		return IsTesting(request) && response.IsSuccessStatusCode
			? await this.ReadAndReturnNewResponse(request, response, cancellationToken).ConfigureAwait(false)
			: response;
	}

	private async Task<HttpResponseMessage> ReadAndReturnNewResponse(HttpRequestMessage request, HttpResponseMessage response, CancellationToken cancellationToken)
	{
		using ArrayPoolMemoryStream memStream = new();

		Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
		await using (stream.ConfigureAwait(false))
		{
			await stream.CopyToAsync(memStream, cancellationToken).ConfigureAwait(false);
		}

		// The status endpoint always returns an object, so any other body means the URL doesn't reach Sonarr's API.
		if (memStream.AsJsonSpan().StartsWith((byte)'{'))
		{
			await ResetContentAsync(response, memStream, cancellationToken).ConfigureAwait(false);
			return response;
		}

		response.Dispose();
		return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
		{
			Content = JsonContent.Create(new
			{
				Message = NOT_API_MESSAGE,
			}, options: _options),
			RequestMessage = request,
			ReasonPhrase = nameof(HttpStatusCode.ServiceUnavailable),
		};
	}

	private static bool IsTesting(HttpRequestMessage request)
	{
		return request is SonarrRequestMessage sonarrRequest && sonarrRequest.IsTest;
	}
	private static async Task ResetContentAsync(HttpResponseMessage response, ArrayPoolMemoryStream stream, CancellationToken token)
	{
		using HttpContent originalContent = response.Content;
		response.Content = new StreamContent(await stream.ToMemoryStreamAsync(token).ConfigureAwait(false));
		originalContent.Headers.CopyTo(response.Content.Headers);
	}
}
