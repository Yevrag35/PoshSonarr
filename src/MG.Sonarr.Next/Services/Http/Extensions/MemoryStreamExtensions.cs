using MG.Sonarr.Next.Services.Http.IO;

namespace MG.Sonarr.Next.Services.Http.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="MemoryStream"/> class.
/// </summary>
internal static class MemoryStreamExtensions
{
	/// <summary>
	/// Provides a method that moves a <see cref="MemoryStream"/> back to its beginning.
	/// </summary>
	/// <param name="stream">The <see cref="MemoryStream"/> to rewind.</param>
	extension(MemoryStream stream)
	{
		/// <summary>
		/// Resets the position of the <see cref="MemoryStream"/> to the beginning.
		/// </summary>
		/// <remarks>If <paramref name="stream"/> is not seekable, the method does nothing.</remarks>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Rewind()
		{
			ArgumentNullException.ThrowIfNull(stream);
			Debug.Assert(stream.CanSeek, "Attempted to rewind a non-seekable stream.");
			if (stream.CanSeek)
			{
				_ = stream.Seek(0, SeekOrigin.Begin);
			}
		}
	}

	/// <summary>
	/// Provides methods that read the buffered contents of an <see cref="ArrayPoolMemoryStream"/>.
	/// </summary>
	/// <param name="stream">The <see cref="ArrayPoolMemoryStream"/> that holds the buffered contents.</param>
	extension(ArrayPoolMemoryStream stream)
	{
		/// <summary>
		/// Copies the contents of the <see cref="ArrayPoolMemoryStream"/> to a new <see cref="MemoryStream"/>.
		/// </summary>
		/// <remarks>
		/// The copy holds every byte written to <paramref name="stream"/>, whatever its position, and it outlives
		/// <paramref name="stream"/>, whose buffer goes back to the pool when it's disposed.
		/// </remarks>
		/// <returns>A new <see cref="MemoryStream"/> that holds the stream's contents, positioned at its start.</returns>
		/// <exception cref="ObjectDisposedException">Thrown when <paramref name="stream"/> has been disposed.</exception>
		internal MemoryStream ToMemoryStream()
		{
			ReadOnlySpan<byte> contents = stream.AsSpan();
			MemoryStream memStream = new(contents.Length);
			memStream.Write(contents);
			memStream.Rewind();

			return memStream;
		}

		/// <summary>
		/// Returns the populated contents of the stream from their first JSON token on, past a UTF-8 byte order mark and any
		/// JSON whitespace.
		/// </summary>
		/// <returns>A read-only span over the stream's contents from their first JSON token, which is empty when the contents
		/// hold nothing else.</returns>
		/// <exception cref="ObjectDisposedException">Thrown when <paramref name="stream"/> has been disposed.</exception>
		internal ReadOnlySpan<byte> AsJsonSpan()
		{
			ReadOnlySpan<byte> json = stream.AsSpan();
			if (json.StartsWith(Encoding.UTF8.Preamble))
			{
				json = json[Encoding.UTF8.Preamble.Length..];
			}

			return json.TrimStart(" \t\r\n"u8);
		}
	}
}
