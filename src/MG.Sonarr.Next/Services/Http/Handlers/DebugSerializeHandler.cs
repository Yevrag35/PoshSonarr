using MG.Sonarr.Next.Services.Http.Extensions;
using MG.Sonarr.Next.Services.Http.IO;
using MG.Sonarr.Next.Strings;
using MG.Sonarr.Resources;

namespace MG.Sonarr.Next.Services.Http.Handlers;

public sealed class DebugSerializeHandler : DelegatingHandler
{
	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return this.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
	}

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

	[StructLayout(LayoutKind.Auto)]
	private readonly struct StringResponse
	{
		public readonly long ContentLength;
		public readonly string JsonString;

		public StringResponse(long contentLength, string jsonString)
		{
			ContentLength = contentLength;
			JsonString = jsonString;
		}
	}
}
