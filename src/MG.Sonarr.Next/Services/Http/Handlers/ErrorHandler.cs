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
			if (TryReadError(memStream, options, out ServerError? error))
			{
				response.AddMetadata(ErrorKey, error);
			}

			response.Content = new StreamContent(await memStream.ToMemoryStreamAsync(token).ConfigureAwait(false));
			originalContent.Headers.CopyTo(response.Content.Headers);

			return response;
		}
		finally
		{
			await disposeTask.ConfigureAwait(false);
		}
	}

	private static bool TryReadError(ArrayPoolMemoryStream stream, JsonSerializerOptions options, [NotNullWhen(true)] out ServerError? error)
	{
		// Sonarr answers a failed validation with an array of errors, which the response reader parses.
		ReadOnlySpan<byte> json = stream.AsJsonSpan();
		if (!json.StartsWith((byte)'{'))
		{
			error = null;
			return false;
		}

		try
		{
			error = JsonSerializer.Deserialize<ServerError>(json, options);
		}
		catch (JsonException)
		{
			error = null; // A malformed body still reaches the response reader as it came.
		}

		return error is not null;
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
