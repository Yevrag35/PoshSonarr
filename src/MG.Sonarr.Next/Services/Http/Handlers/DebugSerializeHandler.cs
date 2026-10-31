using MG.Sonarr.Next.Services.Http.Extensions;
using MG.Sonarr.Next.Services.Http.IO;
using MG.Sonarr.Next.Strings;
using MG.Sonarr.Resources;

namespace MG.Sonarr.Next.Services.Http.Handlers;

/// <summary>
/// Provides a delegating handler that writes request and response bodies to the debug output of the cmdlet that sent
/// the request.
/// </summary>
/// <remarks>
/// <para>The handler finds the sending <see cref="IApiCmdlet"/> in the request's options, where the client that sends
/// the request puts it, and passes a request without one through unchanged. It writes the request body before the send
/// when <see cref="IApiCmdlet.CanDebugSerializeBefore"/> is <see langword="true"/>, and the response body after it when
/// <see cref="IApiCmdlet.CanDebugSerializeAfter"/> is <see langword="true"/>, each preceded by its size in bytes.</para>
/// <para>It buffers a request body before reading it, so the inner handlers send the body from that buffer. It reads a
/// response body into memory and puts a buffered copy back as the response's content, with the original content
/// headers, so the handlers outside it and the response reader can read the body again.</para>
/// <para>The handler calls <see cref="IApiCmdlet.WriteDebugPayload(string)"/> after an <see langword="await"/>, often
/// on a thread-pool thread. It keeps no per-request state, so one instance can serve concurrent requests.</para>
/// </remarks>
public sealed class DebugSerializeHandler : DelegatingHandler
{
	/// <summary>
	/// Sends the request to the inner handler and blocks until its response arrives, writing the bodies to the sending
	/// cmdlet's debug output.
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
	/// Sends the request to the inner handler asynchronously, writing its body and the response's body to the sending
	/// cmdlet's debug output when that cmdlet asks for them.
	/// </summary>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="cancellationToken">A token that cancels the operation.</param>
	/// <returns>A task that completes with the HTTP response message from the inner handler, with its body buffered when
	/// the handler read it.</returns>
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (!ApiCmdletAccessor.TryGetCmdlet(request, out IApiCmdlet? cmdlet))
		{
			return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
		}

		if (cmdlet.CanDebugSerializeBefore && request.Content is not null)
		{
			string requestString = await SerializeRequestAsync(request.Content, cancellationToken).ConfigureAwait(false);
			cmdlet.WriteDebugPayload(requestString);
		}

		HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
		if (cmdlet.CanDebugSerializeAfter && response.Content is not null)
		{
			StringResponse parsed = await SerializeResponseAsync(response, cancellationToken).ConfigureAwait(false);
			string jsonString = Messenger.Format(
				provider: CultureInfo.CurrentCulture,
				format: Messages.Debug_JSONResponse_Preamble,
				parsed.ContentLength, parsed.JsonString);

			cmdlet.WriteDebugPayload(jsonString);
		}

		return response;
	}

	/// <summary>
	/// Buffers a request body and formats it, preceded by its size, for the debug output.
	/// </summary>
	/// <remarks>
	/// The size is the content's <c>Content-Length</c> header, or the body's UTF-8 byte count when the content has no such
	/// header. Since the body is buffered, reading it here leaves it in place for the send.
	/// </remarks>
	/// <param name="content">The request's content.</param>
	/// <param name="token">A token that cancels the read.</param>
	/// <returns>A task that completes with the request body, preceded by its size in bytes, formatted with the current
	/// culture.</returns>
	private static async Task<string> SerializeRequestAsync(HttpContent content, CancellationToken token)
	{
		// A buffered body is sent from its buffer, so reading it here leaves it in place for the send.
		await content.LoadIntoBufferAsync(token).ConfigureAwait(false);
		string jsonString = await content.ReadAsStringAsync(token).ConfigureAwait(false);

		return Messenger.Format(
			provider: CultureInfo.CurrentCulture,
			format: Messages.Debug_JSONRequest_Preamble,
			content.Headers.ContentLength ?? Encoding.UTF8.GetByteCount(jsonString), jsonString);
	}

	/// <summary>
	/// Reads a response body as text and replaces the response's content with a buffered copy of it.
	/// </summary>
	/// <remarks>
	/// The method disposes the original content, and the new content carries a copy of its headers.
	/// </remarks>
	/// <param name="response">The HTTP response message whose body is read.</param>
	/// <param name="token">A token that cancels the read.</param>
	/// <returns>A task that completes with the body's length in bytes and its text, decoded as UTF-8.</returns>
	private static async Task<StringResponse> SerializeResponseAsync(HttpResponseMessage response, CancellationToken token)
	{
		using HttpContent content = response.Content;
		using ArrayPoolMemoryStream memStream = new();

		Stream stream = await content.ReadAsStreamAsync(token).ConfigureAwait(false);

		await stream.CopyToAsync(memStream, token).ConfigureAwait(false);

		long length = memStream.Length;
		string jsonString = memStream.ReadString(Encoding.UTF8);

		memStream.Rewind();
		response.Content = new StreamContent(await memStream.ToMemoryStreamAsync(token).ConfigureAwait(false));
		content.Headers.CopyTo(response.Content.Headers);

		return new(length, jsonString);
	}

	/// <summary>
	/// Represents a response body read for the debug output, with its length in bytes.
	/// </summary>
	/// <param name="ContentLength">The body's length in bytes.</param>
	/// <param name="JsonString">The body, decoded as UTF-8.</param>
	private readonly record struct StringResponse(long ContentLength, string JsonString);
}
