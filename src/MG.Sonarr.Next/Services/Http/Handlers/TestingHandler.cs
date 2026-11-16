using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Services.Http.Extensions;
using MG.Sonarr.Next.Services.Http.IO;
using MG.Sonarr.Next.Services.Http.Requests;

namespace MG.Sonarr.Next.Services.Http.Handlers;

/// <summary>
/// Provides a delegating handler that buffers request bodies and checks that a connection test's response comes from
/// Sonarr's API.
/// </summary>
/// <remarks>
/// <para>The handler puts a buffered copy in place of every request body whose length isn't known, so the body goes out
/// with a <c>Content-Length</c> header, where a JSON body that isn't buffered would go out chunked.</para>
/// <para>It acts on the response only for a request whose <see cref="SonarrRequestMessage.IsTest"/> is
/// <see langword="true"/>. Sonarr's status endpoint always answers with a JSON object, so when such a request succeeds
/// with a body whose first JSON token, past any UTF-8 byte order mark and JSON whitespace, isn't an object, the URL
/// doesn't reach Sonarr's API. The handler then disposes the response and returns a 503 Service Unavailable response
/// whose JSON body's <c>message</c> says so. Otherwise it puts the buffered body back, with the original content
/// headers.</para>
/// <para>The handler overrides only <see cref="Send(HttpRequestMessage, CancellationToken)"/>, since the clients send
/// every request synchronously. A request that reaches it through
/// <see cref="SynchronousHandler.SendAsync(HttpRequestMessage, CancellationToken)"/> instead fails with a
/// <see cref="NotSupportedException"/>. The handler keeps no per-request state, so one instance can serve concurrent
/// requests.</para>
/// </remarks>
internal sealed class TestingHandler : SynchronousHandler
{
	private const string NOT_API_MESSAGE = "The response isn't the JSON object that Sonarr's API returns, so it may be an HTML page. You sure the URL is correct?";

	private readonly JsonSerializerOptions _options;

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
	/// Buffers a request body whose length isn't known, sends the request to the inner handler, and checks the response
	/// of a connection test.
	/// </summary>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="cancellationToken">A token that cancels the operation.</param>
	/// <returns>The HTTP response message from the inner handler, or a 503 response in its place when a connection test's
	/// body isn't a JSON object.</returns>
	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request.Content is { Headers.ContentLength: null })
		{
			// A body whose length isn't known, as a JsonContent's isn't, would go out chunked.
			_ = request.BufferContent(cancellationToken);
		}

		HttpResponseMessage response = base.Send(request, cancellationToken);

		return IsTesting(request) && response.IsSuccessStatusCode
			? this.ReadAndReturnNewResponse(request, response, cancellationToken)
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
	/// <param name="cancellationToken">A token that cancels the read.</param>
	/// <returns><paramref name="response"/>, with its body put back, or a new 503 response when the body isn't a JSON
	/// object.</returns>
	private HttpResponseMessage ReadAndReturnNewResponse(HttpRequestMessage request, HttpResponseMessage response, CancellationToken cancellationToken)
	{
		using ArrayPoolMemoryStream memStream = new();
		response.Content.CopyBodyTo(memStream, cancellationToken);

		// The status endpoint always returns an object, so any other body means the URL doesn't reach Sonarr's API.
		if (memStream.AsJsonSpan().StartsWith((byte)'{'))
		{
			ResetContent(response, memStream);
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
	private static void ResetContent(HttpResponseMessage response, ArrayPoolMemoryStream stream)
	{
		using HttpContent originalContent = response.Content;
		response.Content = new StreamContent(stream.ToMemoryStream());
		originalContent.Headers.CopyTo(response.Content.Headers);
	}
}
