using MG.Sonarr.Next.Services.Http.IO;

namespace MG.Sonarr.Next.Services.Http.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="MemoryStream"/> class.
/// </summary>
internal static class MemoryStreamExtensions
{
	/// <summary>
	/// Resets the position of the specified <see cref="MemoryStream"/> to the beginning.
	/// </summary>
	/// <remarks>If the <paramref name="stream"/> is not seekable, the method does nothing.</remarks>
	/// <param name="stream">The <see cref="MemoryStream"/> to rewind..</param>
	/// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Rewind(this MemoryStream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);
		Debug.Assert(stream.CanSeek, "Attempted to rewind a non-seekable stream.");
		_ = stream.Seek(0, SeekOrigin.Begin);
	}

	internal static async Task<MemoryStream> ToMemoryStreamAsync(this ArrayPoolMemoryStream stream, CancellationToken cancellationToken)
	{
		if (!stream.CanSeek)
			throw new InvalidOperationException("Stream is not seekable.");

		stream.Rewind();
		MemoryStream memStream = new(checked((int)stream.Length));

		await stream.CopyToAsync(memStream, cancellationToken).ConfigureAwait(false);
		memStream.Rewind();

		return memStream;
	}

	/// <summary>
	/// Returns the populated contents of the stream from their first JSON token on, past a UTF-8 byte order mark and any
	/// JSON whitespace.
	/// </summary>
	/// <param name="stream">The stream that holds a buffered body.</param>
	/// <returns>A read-only span over the stream's contents from their first JSON token, which is empty when the contents
	/// hold nothing else.</returns>
	/// <exception cref="ObjectDisposedException">Thrown when <paramref name="stream"/> has been disposed.</exception>
	internal static ReadOnlySpan<byte> AsJsonSpan(this ArrayPoolMemoryStream stream)
	{
		ReadOnlySpan<byte> json = stream.AsMemory().Span;
		if (json.StartsWith(Encoding.UTF8.Preamble))
		{
			json = json[Encoding.UTF8.Preamble.Length..];
		}

		return json.TrimStart(" \t\r\n"u8);
	}
}
