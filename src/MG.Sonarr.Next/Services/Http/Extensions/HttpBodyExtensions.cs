using MG.Sonarr.Next.Services.Http.IO;

namespace MG.Sonarr.Next.Services.Http.Extensions;

/// <summary>
/// Provides extension methods that read and buffer the bodies of HTTP requests and responses synchronously.
/// </summary>
/// <remarks>
/// <para>
/// A synchronous read takes no token, so a read that waits on the server, such as one of a body that the server stops
/// sending partway through, waits until the server closes the connection. Each method disposes the content when its token
/// is canceled, as <see cref="HttpClient"/> does while it buffers a body, which ends such a read, and reports the read's
/// failure as an <see cref="OperationCanceledException"/> for the token. <see cref="HttpClient"/> turns that exception into
/// a timeout when its own timeout canceled the token.
/// </para>
/// <para>
/// A canceled read of a response's body ends when the response's connection closes. The module's primary handler drains
/// no body, so it closes the connection as soon as the content is disposed. A primary handler that drains the rest of a
/// body first, as one does by default for up to two seconds, puts off the end of the read by as long as the drain takes.
/// </para>
/// </remarks>
internal static class HttpBodyExtensions
{
	/// <summary>
	/// Provides methods that read the body of an <see cref="HttpContent"/> synchronously.
	/// </summary>
	/// <param name="content">The content whose body is read.</param>
	extension(HttpContent content)
	{
		/// <summary>
		/// Copies the body to the specified stream synchronously.
		/// </summary>
		/// <remarks>
		/// The content writes its body into <paramref name="destination"/>, so content that builds its body as it's read,
		/// such as a <see cref="System.Net.Http.Json.JsonContent"/>, serializes the body here, once.
		/// </remarks>
		/// <param name="destination">The stream that receives the body.</param>
		/// <param name="token">A token that cancels the copy, and disposes the content when it's canceled.</param>
		/// <exception cref="OperationCanceledException">Thrown when <paramref name="token"/> is canceled before or during the
		/// copy.</exception>
		internal void CopyBodyTo(Stream destination, CancellationToken token)
		{
			token.ThrowIfCancellationRequested();
			using CancellationTokenRegistration registration = DisposeOnCancel(content, token);
			try
			{
				content.CopyTo(destination, context: null, token);
			}
			catch (Exception e) when (IsCanceledRead(e, token))
			{
				throw new OperationCanceledException(e.Message, e, token);
			}
		}

		/// <summary>
		/// Reads the body synchronously as a string, decoded with the character set that the content's <c>Content-Type</c>
		/// header names.
		/// </summary>
		/// <remarks>
		/// A byte order mark decides the encoding when the header names no character set, and UTF-8 decodes the body when
		/// there's neither, as <see cref="HttpContent.ReadAsStringAsync()"/> does. A byte order mark isn't part of the
		/// string.
		/// </remarks>
		/// <param name="token">A token that cancels the read, and disposes the content when it's canceled.</param>
		/// <returns>The body's text, which is empty when the body is.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the <c>Content-Type</c> header names a character set that
		/// .NET doesn't know.</exception>
		/// <exception cref="OperationCanceledException">Thrown when <paramref name="token"/> is canceled before or during the
		/// read.</exception>
		internal string ReadBodyAsString(CancellationToken token)
		{
			Encoding encoding = GetCharsetEncoding(content) ?? Encoding.UTF8;

			token.ThrowIfCancellationRequested();
			using CancellationTokenRegistration registration = DisposeOnCancel(content, token);
			try
			{
				using StreamReader reader = new(content.ReadAsStream(token), encoding, detectEncodingFromByteOrderMarks: true);
				return reader.ReadToEnd();
			}
			catch (Exception e) when (IsCanceledRead(e, token))
			{
				throw new OperationCanceledException(e.Message, e, token);
			}
		}

		/// <summary>
		/// Deserializes the body synchronously from JSON as the specified type.
		/// </summary>
		/// <remarks>
		/// A body in a character set other than UTF-8, as the content's <c>Content-Type</c> header names it, is transcoded
		/// to UTF-8 as it's read, as <c>ReadFromJsonAsync</c> does.
		/// </remarks>
		/// <typeparam name="T">The type to deserialize the body as.</typeparam>
		/// <param name="options">The options that deserialize the body, or <see langword="null"/> for the defaults.</param>
		/// <param name="token">A token that cancels the read, and disposes the content when it's canceled.</param>
		/// <returns>The deserialized value, or <see langword="null"/> when the body is the JSON literal <c>null</c>.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the <c>Content-Type</c> header names a character set that
		/// .NET doesn't know.</exception>
		/// <exception cref="JsonException">Thrown when the body isn't valid JSON, or when its JSON doesn't fit
		/// <typeparamref name="T"/>.</exception>
		/// <exception cref="OperationCanceledException">Thrown when <paramref name="token"/> is canceled before or during the
		/// read.</exception>
		internal T? ReadBodyFromJson<T>(JsonSerializerOptions? options, CancellationToken token)
		{
			Encoding? encoding = GetCharsetEncoding(content);

			token.ThrowIfCancellationRequested();
			using CancellationTokenRegistration registration = DisposeOnCancel(content, token);
			try
			{
				Stream stream = content.ReadAsStream(token);
				if (encoding is not null && encoding.CodePage != Encoding.UTF8.CodePage)
				{
					stream = Encoding.CreateTranscodingStream(stream, encoding, Encoding.UTF8);
				}

				using (stream)
				{
					return JsonSerializer.Deserialize<T>(stream, options);
				}
			}
			catch (Exception e) when (IsCanceledRead(e, token))
			{
				throw new OperationCanceledException(e.Message, e, token);
			}
		}
	}

	/// <summary>
	/// Provides a method that buffers the body of an <see cref="HttpRequestMessage"/>.
	/// </summary>
	/// <param name="request">The request whose body is buffered.</param>
	extension(HttpRequestMessage request)
	{
		/// <summary>
		/// Puts a copy of the request's body, in a pooled buffer, in place of its content, with the original content
		/// headers and a <c>Content-Length</c> header, and returns the buffer.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The original content writes its body into the buffer once, and the method disposes it. The body goes out from the
		/// buffer with a <c>Content-Length</c> header, where a body whose length isn't known goes out chunked. The method
		/// sets the header to the body's length rather than leaving the new content to compute it from the buffer, so the
		/// header keeps its value after a read through <see cref="HttpContent.ReadAsStream()"/> has disposed the buffer.
		/// </para>
		/// <para>
		/// The new content owns the buffer, which goes back to the pool when the request is disposed. The returned stream is
		/// for reading the body until then. The request's first send starts at the buffer's position, so the caller reads
		/// the body without moving it, as <see cref="ArrayPoolMemoryStream.ReadString(Encoding)"/> and
		/// <see cref="ArrayPoolMemoryStream.AsSpan()"/> do.
		/// </para>
		/// <para>
		/// The request's content isn't <see langword="null"/>; the caller checks.
		/// </para>
		/// </remarks>
		/// <param name="token">A token that cancels the copy.</param>
		/// <returns>The stream that holds the body, which the request's new content owns.</returns>
		/// <exception cref="OperationCanceledException">Thrown when <paramref name="token"/> is canceled before or during the
		/// copy.</exception>
		internal ArrayPoolMemoryStream BufferContent(CancellationToken token)
		{
			Debug.Assert(request.Content is not null, "The caller checks that the request has content.");
			HttpContent original = request.Content!;

			ArrayPoolMemoryStream buffer = new();
			StreamContent copy;
			try
			{
				original.CopyBodyTo(buffer, token);
				buffer.Rewind();
				copy = new StreamContent(buffer);
			}
			catch
			{
				buffer.Dispose();
				throw;
			}

			// The setter replaces a Content-Length that the copy took from the original's headers.
			original.Headers.CopyTo(copy.Headers);
			copy.Headers.ContentLength = buffer.Length;
			request.Content = copy;
			original.Dispose();

			return buffer;
		}
	}

	/// <summary>
	/// Registers a callback that disposes the specified content when the specified token is canceled.
	/// </summary>
	/// <param name="content">The content to dispose.</param>
	/// <param name="token">The token whose cancellation disposes the content.</param>
	/// <returns>The registration, which the caller disposes once its read has ended.</returns>
	private static CancellationTokenRegistration DisposeOnCancel(HttpContent content, CancellationToken token)
	{
		return token.UnsafeRegister(static state => ((HttpContent)state!).Dispose(), content);
	}

	/// <summary>
	/// Gets the encoding of the character set that the specified content's <c>Content-Type</c> header names.
	/// </summary>
	/// <remarks>
	/// A character set in quotes loses one pair of them, as <see cref="HttpContent"/> and <c>ReadFromJsonAsync</c> read it.
	/// </remarks>
	/// <param name="content">The content whose header is read.</param>
	/// <returns>The encoding, or <see langword="null"/> when the header names no character set.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the header names a character set that .NET doesn't
	/// know.</exception>
	private static Encoding? GetCharsetEncoding(HttpContent content)
	{
		string? charset = content.Headers.ContentType?.CharSet;
		if (charset is null)
		{
			return null;
		}

		if (charset.Length > 2 && charset[0] == '"' && charset[^1] == '"')
		{
			charset = charset[1..^1];
		}

		try
		{
			return Encoding.GetEncoding(charset);
		}
		catch (ArgumentException e)
		{
			throw new InvalidOperationException($"The character set '{charset}' in the content's Content-Type header isn't one that .NET knows.", e);
		}
	}

	/// <summary>
	/// Determines whether an exception from a read means that the read's token was canceled.
	/// </summary>
	/// <remarks>
	/// A canceled token disposes the content, so the read fails with whatever a read of disposed content throws, such as
	/// an <see cref="ObjectDisposedException"/> or an <see cref="HttpRequestException"/>. An
	/// <see cref="OperationCanceledException"/> already says what happened, so it passes through as it is.
	/// </remarks>
	/// <param name="exception">The exception that the read threw.</param>
	/// <param name="token">The read's token.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="token"/> is canceled and <paramref name="exception"/> isn't an
	/// <see cref="OperationCanceledException"/>; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool IsCanceledRead(Exception exception, CancellationToken token)
	{
		return token.IsCancellationRequested && exception is not OperationCanceledException;
	}
}
