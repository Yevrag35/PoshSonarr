using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Services.Http.Extensions;
using MG.Sonarr.Next.Services.Http.IO;
using MG.Sonarr.Next.Services.Http.Requests;
using System.Net;
using System.Net.Http.Json;

namespace MG.Sonarr.Next.Services.Http.Handlers;

/// <summary>
/// Provides a delegating handler that buffers request bodies and checks that a connection test's response comes from
/// Sonarr's API.
/// </summary>
/// <remarks>
/// <para>The handler buffers every request body, so the body goes out with a <c>Content-Length</c> header, where an
/// unbuffered JSON body would go out chunked.</para>
/// <para>It acts on the response only for a request whose <see cref="SonarrRequestMessage.IsTest"/> is
/// <see langword="true"/>. Sonarr's status endpoint always answers with a JSON object, so when such a request succeeds
/// with a body whose first JSON token, past any UTF-8 byte order mark and JSON whitespace, isn't an object, the URL
/// doesn't reach Sonarr's API. The handler then disposes the response and returns a 503 Service Unavailable response
/// whose JSON body's <c>message</c> says so. Otherwise it puts the buffered body back, with the original content
/// headers.</para>
/// <para>The handler keeps no per-request state, so one instance can serve concurrent requests.</para>
/// </remarks>
public sealed class TestingHandler : DelegatingHandler
{
	const string NOT_API_MESSAGE = "The response isn't the JSON object that Sonarr's API returns, so it may be an HTML page. You sure the URL is correct?";

	readonly JsonSerializerOptions _options;

	/// <summary>
	/// Initializes a new <see cref="TestingHandler"/> instance with the specified JSON options.
	/// </summary>
	/// <param name="options">The JSON options whose <see cref="ISonarrJsonOptions.ForSerializing"/> options write the
	/// body of the response that replaces one that doesn't come from Sonarr's API.</param>
	public TestingHandler(ISonarrJsonOptions options)
	{
		_options = options.ForSerializing;
	}

	/// <summary>
	/// Buffers the request body, sends the request to the inner handler, and blocks until its response arrives.
	/// </summary>
	/// <remarks>
	/// The method blocks on <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>, so the inner handlers run on
	/// their asynchronous path.
	/// </remarks>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="cancellationToken">A token that cancels the operation.</param>
	/// <returns>The HTTP response message from the inner handler, or a 503 response in its place when a connection test's
	/// body isn't a JSON object.</returns>
	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return this.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
	}

	/// <summary>
	/// Buffers the request body and sends the request to the inner handler asynchronously, checking the response of a
	/// connection test.
	/// </summary>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="cancellationToken">A token that cancels the operation.</param>
	/// <returns>A task that completes with the HTTP response message from the inner handler, or with a 503 response in its
	/// place when a connection test's body isn't a JSON object.</returns>
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

	/// <summary>
	/// Buffers a successful connection test's response body, and returns the response with that body when it's a JSON
	/// object, or a 503 response in its place otherwise.
	/// </summary>
	/// <remarks>
	/// The method disposes <paramref name="response"/> when it replaces it.
	/// </remarks>
	/// <param name="request">The connection test's request, which the replacement response refers to.</param>
	/// <param name="response">The successful response to the connection test.</param>
	/// <param name="cancellationToken">A token that cancels the operation.</param>
	/// <returns>A task that completes with <paramref name="response"/>, with its body put back, or with a new 503 response
	/// when the body isn't a JSON object.</returns>
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

	/// <summary>
	/// Determines whether a request is a connection test.
	/// </summary>
	/// <param name="request">The HTTP request message to check.</param>
	/// <returns><see langword="true"/> if <paramref name="request"/> is a <see cref="SonarrRequestMessage"/> whose
	/// <see cref="SonarrRequestMessage.IsTest"/> is <see langword="true"/>; otherwise, <see langword="false"/>.</returns>
	private static bool IsTesting(HttpRequestMessage request)
	{
		return request is SonarrRequestMessage sonarrRequest && sonarrRequest.IsTest;
	}
	/// <summary>
	/// Replaces a response's content with a copy of its buffered body and its original content headers.
	/// </summary>
	/// <remarks>
	/// The method disposes the original content.
	/// </remarks>
	/// <param name="response">The HTTP response message whose content is replaced.</param>
	/// <param name="stream">The stream that holds the buffered body.</param>
	/// <param name="token">A token that cancels the copy.</param>
	/// <returns>A task that completes when the content has been replaced.</returns>
	private static async Task ResetContentAsync(HttpResponseMessage response, ArrayPoolMemoryStream stream, CancellationToken token)
	{
		using HttpContent originalContent = response.Content;
		response.Content = new StreamContent(await stream.ToMemoryStreamAsync(token).ConfigureAwait(false));
		originalContent.Headers.CopyTo(response.Content.Headers);
	}
}
