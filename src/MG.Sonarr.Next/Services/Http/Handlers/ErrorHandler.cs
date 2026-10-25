using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Services.Http.Extensions;
using MG.Sonarr.Next.Services.Http.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Services.Http.Handlers;

internal sealed class ErrorHandler : DelegatingHandler
{
	internal const string Is404 = "Is404";
	internal const string ErrorKey = "ResponseError";
	internal const HttpStatusCode NoResponseCode = (HttpStatusCode)600;
	private const int STREAM_BUFFER_SIZE = 2048;
	private const string JSON = "json";

	private readonly JsonSerializerOptions _options;

	public ErrorHandler(ISonarrJsonOptions jsonOptions)
	{
		_options = jsonOptions.ForDeserializing;
	}

	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return this.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
	}

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

		if (!response.IsSuccessStatusCode)
		{
			if (response.StatusCode == HttpStatusCode.NotFound)
			{
				response.AddMetadata(Is404, value: null);
				return response;
			}

			response.RequestMessage ??= request;
			response = await ReadErrorAsync(response, _options, cancellationToken).ConfigureAwait(false);
		}

		return response;
	}

	private static bool IsJsonType([NotNullWhen(true)] MediaTypeHeaderValue? mediaType)
	{
		return mediaType?.MediaType is string type
			&& type.TryLastIndexOf(JSON, StringComparison.OrdinalIgnoreCase, out int index)
			&& index != 0
			&& type[index - 1] is '/' or '+';
	}

	private static async Task<HttpResponseMessage> ReadErrorAsync(HttpResponseMessage response, JsonSerializerOptions options, CancellationToken token)
	{
		if (!IsJsonType(response.Content.Headers.ContentType))
		{
			return response; // Not a JSON error response, return as is
		}

		using HttpContent originalContent = response.Content;
		Stream stream = await originalContent.ReadAsStreamAsync(token).ConfigureAwait(false);

		using ArrayPoolMemoryStream memStream = new();
		await stream.CopyToAsync(memStream, cancellationToken: token).ConfigureAwait(false);

		ValueTask disposeTask = stream.DisposeAsync();
		try
		{
			memStream.Rewind();

			var error = await JsonSerializer.DeserializeAsync<ServerError>(memStream, options, token).ConfigureAwait(false)
				?? throw new JsonException("Unable to deserialize the error response content.");

			response.AddMetadata(ErrorKey, error);

			memStream.Rewind();
			response.Content = new StreamContent(await memStream.ToMemoryStreamAsync(token).ConfigureAwait(false));
			originalContent.Headers.CopyTo(response.Content.Headers);

			return response;
		}
		finally
		{
			await disposeTask.ConfigureAwait(false);
		}
	}

	private sealed class ServerError : IServerError
	{
		public string? Description { get; set; }

		public string? Message { get; set; }

		public string? Title { get; set; }

		[JsonPropertyName("status")]
		public int? StatusCode { get; set; }

		public string? TraceId { get; set; }

		public ServerError() { }
	}
}
