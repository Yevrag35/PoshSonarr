using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Services.Http.Extensions;
using MG.Sonarr.Next.Services.Http.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Services.Http.Handlers;

/// <summary>
/// Provides a delegating handler that marks 404 responses and parses the server error from the JSON body of other
/// error responses.
/// </summary>
/// <remarks>
/// <para>A 404 Not Found response passes through unchanged, with <see cref="Is404"/> added to its request's options
/// for the response reader.</para>
/// <para>Any other error response with a JSON content type has its body buffered, and a body that is one JSON object is
/// parsed into a server error, which goes into the request's options under <see cref="ErrorKey"/>. Whatever the body
/// holds, one error object, the array of validation failures that Sonarr returns for a 400, or JSON that doesn't parse,
/// the response reaches <see cref="SonarrResponseReader"/> with its body and content headers intact.</para>
/// <para>The handler keeps no per-request state, so one instance can serve concurrent requests.</para>
/// </remarks>
internal sealed class ErrorHandler : DelegatingHandler
{
	/// <summary>
	/// The key of the request option that marks a response whose status is 404 Not Found.
	/// </summary>
	internal const string Is404 = "Is404";
	/// <summary>
	/// The key of the request option that holds the server error parsed from an error response's body.
	/// </summary>
	internal const string ErrorKey = "ResponseError";
	/// <summary>
	/// The status code that a result takes when its request got no response.
	/// </summary>
	internal const HttpStatusCode NoResponseCode = (HttpStatusCode)600;
	private const string JSON = "json";

	private readonly JsonSerializerOptions _options;

	/// <summary>
	/// Initializes a new <see cref="ErrorHandler"/> instance with the specified JSON options.
	/// </summary>
	/// <param name="jsonOptions">The JSON options whose <see cref="ISonarrJsonOptions.ForDeserializing"/> options parse
	/// the server error.</param>
	public ErrorHandler(ISonarrJsonOptions jsonOptions)
	{
		_options = jsonOptions.ForDeserializing;
	}

	/// <summary>
	/// Sends the request to the inner handler and blocks until its response arrives, reading the server error from an
	/// error response.
	/// </summary>
	/// <remarks>
	/// The method blocks on <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>, so the inner handlers run on
	/// their asynchronous path.
	/// </remarks>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="cancellationToken">A token that cancels the operation.</param>
	/// <returns>The HTTP response message from the inner handler, with its body buffered when the handler read
	/// it.</returns>
	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return this.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
	}

	/// <summary>
	/// Sends the request to the inner handler asynchronously, marking a 404 response and reading the server error from
	/// any other error response.
	/// </summary>
	/// <remarks>
	/// An error response that has no request message gets <paramref name="request"/> as its own before its body is read.
	/// </remarks>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="cancellationToken">A token that cancels the operation.</param>
	/// <returns>A task that completes with the HTTP response message from the inner handler, with its body buffered when
	/// the handler read it.</returns>
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

	/// <summary>
	/// Determines whether a media type is JSON, with a <c>json</c> subtype or a <c>+json</c> suffix.
	/// </summary>
	/// <remarks>
	/// The comparison is ordinal and ignores case, and it looks at the last <c>json</c> in the media type.
	/// </remarks>
	/// <param name="mediaType">The response's content type, or <see langword="null"/> when it has none.</param>
	/// <returns><see langword="true"/> if the media type's last <c>json</c> follows a <c>/</c> or a <c>+</c>; otherwise,
	/// <see langword="false"/>.</returns>
	private static bool IsJsonType([NotNullWhen(true)] MediaTypeHeaderValue? mediaType)
	{
		return mediaType?.MediaType is string type
			&& type.TryLastIndexOf(JSON, StringComparison.OrdinalIgnoreCase, out int index)
			&& index != 0
			&& type[index - 1] is '/' or '+';
	}

	/// <summary>
	/// Buffers a JSON error response's body, stores the server error that it holds in the request's options, and puts
	/// the body back as the response's content.
	/// </summary>
	/// <remarks>
	/// The method disposes the original content, and the new content carries a copy of its headers. A response whose
	/// content type isn't JSON comes back untouched.
	/// </remarks>
	/// <param name="response">The error response whose body is read.</param>
	/// <param name="options">The JSON options that parse the server error.</param>
	/// <param name="token">A token that cancels the read.</param>
	/// <returns>A task that completes with <paramref name="response"/>, with its body buffered when its content type is
	/// JSON.</returns>
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

	/// <summary>
	/// Attempts to parse a buffered body that is one JSON object as a server error.
	/// </summary>
	/// <remarks>
	/// A body that is a JSON array, such as Sonarr's validation failures, or that doesn't parse, gives no error and
	/// throws nothing, so the response reader still gets it as it came.
	/// </remarks>
	/// <param name="stream">The stream that holds the buffered body.</param>
	/// <param name="options">The JSON options that parse the body.</param>
	/// <param name="error">When this method returns, the parsed server error, or <see langword="null"/> when the body
	/// isn't one JSON object or doesn't parse.</param>
	/// <returns><see langword="true"/> if the body parsed as a server error; otherwise,
	/// <see langword="false"/>.</returns>
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

	/// <summary>
	/// Represents the error object that Sonarr returns in the body of an error response.
	/// </summary>
	private sealed class ServerError : IServerError
	{
		/// <summary>
		/// Gets or sets the error's description.
		/// </summary>
		/// <value>The description, or <see langword="null"/> when the body has none.</value>
		public string? Description { get; set; }

		/// <summary>
		/// Gets or sets the error's message.
		/// </summary>
		/// <value>The message, or <see langword="null"/> when the body has none.</value>
		public string? Message { get; set; }

		/// <summary>
		/// Gets or sets the error's title.
		/// </summary>
		/// <value>The title, or <see langword="null"/> when the body has none.</value>
		public string? Title { get; set; }

		/// <summary>
		/// Gets or sets the status code that the body's <c>status</c> field gives.
		/// </summary>
		/// <value>The status code, or <see langword="null"/> when the body has none.</value>
		[JsonPropertyName("status")]
		public int? StatusCode { get; set; }

		/// <summary>
		/// Gets or sets the identifier of the server's trace of the failed request.
		/// </summary>
		/// <value>The trace identifier, or <see langword="null"/> when the body has none.</value>
		public string? TraceId { get; set; }

		/// <summary>
		/// Initializes a new <see cref="ServerError"/> instance with no values set.
		/// </summary>
		public ServerError() { }
	}
}
