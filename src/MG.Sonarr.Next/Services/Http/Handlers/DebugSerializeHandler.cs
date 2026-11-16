using MG.Sonarr.Next.Services.Http.Extensions;
using MG.Sonarr.Next.Services.Http.IO;

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
/// <para>It reads a request body from a buffered copy that it puts in place of the request's content, so the content
/// serializes its body once and the inner handlers send the body from that buffer. It reads a response body into memory
/// and puts a buffered copy back as the response's content, with the original content headers, so the handlers outside
/// it and the response reader can read the body again.</para>
/// <para>The handler overrides only <see cref="Send(HttpRequestMessage, CancellationToken)"/>, since the clients send
/// every request synchronously, so it calls <see cref="IApiCmdlet.WriteDebugPayload(string)"/> on the thread that sent
/// the request, which for a cmdlet is its pipeline thread. A request that goes out through
/// <see cref="DelegatingHandler.SendAsync(HttpRequestMessage, CancellationToken)"/> instead passes through it without
/// its output. The handler keeps no per-request state, so one instance can serve concurrent requests.</para>
/// </remarks>
internal sealed class DebugSerializeHandler : DelegatingHandler
{
	/// <summary>
	/// Sends the request to the inner handler, writing its body and the response's body to the sending cmdlet's debug
	/// output when that cmdlet asks for them.
	/// </summary>
	/// <remarks>
	/// When reading or writing the response's body throws, as a cmdlet's own debug output does under a preference of
	/// <see cref="ActionPreference.Stop"/>, the method disposes the response before the exception leaves it.
	/// </remarks>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="cancellationToken">A token that cancels the operation.</param>
	/// <returns>The HTTP response message from the inner handler, with its body buffered when the handler read
	/// it.</returns>
	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (!ApiCmdletAccessor.TryGetCmdlet(request, out IApiCmdlet? cmdlet))
		{
			return base.Send(request, cancellationToken);
		}

		if (cmdlet.CanDebugSerializeBefore && request.Content is not null)
		{
			cmdlet.WriteDebugPayload(SerializeRequest(request, cancellationToken));
		}

		HttpResponseMessage response = base.Send(request, cancellationToken);
		if (cmdlet.CanDebugSerializeAfter && response.Content is not null)
		{
			try
			{
				StringResponse parsed = SerializeResponse(response, cancellationToken);
				string jsonString = string.Format(
					provider: CultureInfo.CurrentCulture,
					format: Messages.Debug_JSONResponse_Preamble,
					parsed.ContentLength, parsed.JsonString);

				cmdlet.WriteDebugPayload(jsonString);
			}
			catch
			{
				response.Dispose();
				throw;
			}
		}

		return response;
	}

	/// <summary>
	/// Puts a buffered copy of a request's body in place of its content, and formats the body, preceded by its size, for
	/// the debug output.
	/// </summary>
	/// <remarks>
	/// The inner handlers send the body from the copy, so the content serializes its body once, here. The body is decoded
	/// as UTF-8, the encoding of the JSON bodies that the clients send.
	/// </remarks>
	/// <param name="request">The request whose body is read, which has content.</param>
	/// <param name="token">A token that cancels the copy.</param>
	/// <returns>The request body, preceded by its size in bytes, formatted with the current culture.</returns>
	private static string SerializeRequest(HttpRequestMessage request, CancellationToken token)
	{
		ArrayPoolMemoryStream buffer = request.BufferContent(token);

		return string.Format(
			provider: CultureInfo.CurrentCulture,
			format: Messages.Debug_JSONRequest_Preamble,
			buffer.Length, buffer.ReadString(Encoding.UTF8));
	}

	/// <summary>
	/// Reads a response body as text and replaces the response's content with a buffered copy of it.
	/// </summary>
	/// <remarks>
	/// The method disposes the original content, and the new content carries a copy of its headers.
	/// </remarks>
	/// <param name="response">The HTTP response message whose body is read.</param>
	/// <param name="token">A token that cancels the read.</param>
	/// <returns>The body's length in bytes and its text, decoded as UTF-8.</returns>
	private static StringResponse SerializeResponse(HttpResponseMessage response, CancellationToken token)
	{
		using HttpContent content = response.Content;
		using ArrayPoolMemoryStream memStream = new();

		content.CopyBodyTo(memStream, token);

		long length = memStream.Length;
		string jsonString = memStream.ReadString(Encoding.UTF8);

		response.Content = new StreamContent(memStream.ToMemoryStream());
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
