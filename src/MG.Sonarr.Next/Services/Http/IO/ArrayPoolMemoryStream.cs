using MG.Sonarr.Next.Buffers.Intrinsics;
using MG.Sonarr.Next.Guarding;

namespace MG.Sonarr.Next.Services.Http.IO;

/// <summary>
/// Provides a memory stream that rents a buffer from an array pool, allowing for efficient memory management and reuse.
/// </summary>
/// <remarks>
/// <para>This class is designed to minimize memory allocations by utilizing an array pool. It supports reading
/// and writing operations, and the buffer can be cleared before returning to the pool based on the ClearOnReturn
/// property. The stream's length and position can be managed, and it provides methods to convert the stream contents to
/// an array or an <see cref="ArraySlice{T}"/>.</para>
/// <para>Once the stream is disposed, its buffer is back in the pool, so <see cref="ToArray"/>,
/// <see cref="SetLength(long)"/>, and <see cref="Position"/> throw an <see cref="ObjectDisposedException"/> whatever
/// their arguments, and <see cref="TryGetBuffer(out ArraySlice{byte})"/> returns <see langword="false"/>. A disposed
/// <see cref="MemoryStream"/> still gives its contents from <see cref="MemoryStream.ToArray"/> and
/// <see cref="MemoryStream.TryGetBuffer(out ArraySegment{byte})"/>, throws a <see cref="NotSupportedException"/> from
/// <see cref="MemoryStream.SetLength(long)"/>, and checks a new length or position before it checks whether it's
/// disposed.</para>
/// </remarks>
[DebuggerDisplay(@"\{Length = {Length}, Position = {Position}, Capacity = {Capacity}\}"), DebuggerStepThrough]
internal sealed class ArrayPoolMemoryStream : Stream
{
	private const int DEFAULT_INITIAL_CAPACITY = 8192;

	private readonly ArrayPool<byte> _pool;
	private byte[]? _buffer;              // null => disposed
	private int _length;
	private int _position;

	/// <summary>
	/// Occurs when the stream is disposed, allowing subscribers to be notified before the underlying buffer is returned to the pool.
	/// </summary>
	internal event Action? Disposed;

	/// <summary>
	/// Initializes a new instance of the <see cref="ArrayPoolMemoryStream"/> class with the specified initial capacity and array pool.
	/// </summary>
	/// <param name="initialCapacity">Initial capacity in bytes; rounded up to at least 1.</param>
	/// <param name="pool">Pool to rent from; defaults to <see cref="ArrayPool{T}.Shared"/>.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="initialCapacity"/> is negative.</exception>
	[DebuggerStepThrough]
	public ArrayPoolMemoryStream(int initialCapacity = DEFAULT_INITIAL_CAPACITY, ArrayPool<byte>? pool = null)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);

		_pool = pool ?? ArrayPool<byte>.Shared;
		_buffer = _pool.Rent(Math.Max(16, initialCapacity));
		_length = 0;
		_position = 0;
	}

	/// <summary>
	/// If <see langword="true"/>, the rented buffer is cleared before returning to the pool.
	/// Default is <see langword="false"/>.
	/// </summary>
	public bool ClearOnReturn { get; set; }

	/// <summary>
	/// The current capacity of the underlying rented buffer.
	/// </summary>
	public int Capacity => _buffer?.Length ?? 0;

	///// <summary>
	///// Gets a value that indicates whether <see cref="Retire"/> has been called, which closes the stream to new views and
	///// returns its buffer to the pool once the last view is disposed.
	///// </summary>
	///// <value><see langword="true"/> if the stream is retired; otherwise, <see langword="false"/>.</value>
	//public bool IsRetired => (Volatile.Read(ref _viewState) & RETIRED_FLAG) != 0;

	/// <inheritdoc/>
	[MemberNotNullWhen(true, nameof(_buffer))]
	public override bool CanRead => _buffer is not null;

	/// <inheritdoc/>
	[MemberNotNullWhen(true, nameof(_buffer))]
	public override bool CanSeek => _buffer is not null;

	/// <inheritdoc/>
	[MemberNotNullWhen(true, nameof(_buffer))]
	public override bool CanWrite => _buffer is not null;

	/// <inheritdoc/>
	public override long Length
	{
		get { this.EnsureNotDisposed(); return _length; }
	}

	/// <inheritdoc/>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in nameof()")]
	public override long Position
	{
		get { this.EnsureNotDisposed(); return _position; }
		set
		{
			this.EnsureNotDisposed();
			ThrowHelper.ThrowIfNegativeOrGreaterThan(value, int.MaxValue, nameof(Position));
			_position = (int)value;
		}
	}

	/// <summary>
	/// Returns the stream contents as a new array of length <see cref="Length"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="MemoryStream.ToArray"/> still works on a disposed stream, while this stream's buffer is back in the pool
	/// once it's disposed, so the method throws instead.
	/// </remarks>
	/// <returns>A new array that holds the bytes written to the stream.</returns>
	/// <inheritdoc cref="EnsureNotDisposed" path="/exception"/>
	public byte[] ToArray()
	{
		this.EnsureNotDisposed();
		if (_length == 0)
			return [];

		byte[] result = new byte[_length];
		Buffer.BlockCopy(_buffer, 0, result, 0, _length);

		return result;
	}

	/// <summary>
	/// Returns an <see cref="ArraySlice{T}"/> over the live pooled buffer for the readable portion [0, <see cref="Length"/>).
	/// Use with care; the segment is invalid after dispose or any expansion.
	/// </summary>
	/// <remarks>
	/// <see cref="MemoryStream.TryGetBuffer(out ArraySegment{byte})"/> still gives the buffer of a disposed stream, while
	/// this stream's buffer is back in the pool once it's disposed, so a disposed stream gives none.
	/// </remarks>
	/// <param name="segment">
	/// When this method returns, a slice over the stream's contents, or an empty slice when the stream has been disposed.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the stream hasn't been disposed; otherwise, <see langword="false"/>.
	/// </returns>
	public bool TryGetBuffer(out ArraySlice<byte> segment)
	{
		if (_buffer is not null)
		{
			unsafe
			{
				// SAFETY: Every write and SetLength makes room in _buffer before it raises _length, so _length never exceeds
				// _buffer.Length while no other thread uses the stream.
				segment = ArraySliceMarshal.Create(_buffer, 0, _length);
			}

			return true;
		}

		segment = [];
		return false;
	}

	/// <summary>
	/// Returns the populated contents of the stream as a span over the underlying buffer.
	/// </summary>
	/// <remarks>
	/// <para>The span covers the bytes from the start of the stream up to <see cref="Length"/>. Changing a byte through it
	/// changes the stream's contents, while <see cref="Write(ReadOnlySpan{byte})"/> and its overloads are what add bytes.</para>
	/// <para>The span aliases the pooled buffer, so a write that grows the stream, or disposing the stream, leaves the span
	/// over an array that's back in the pool.</para>
	/// </remarks>
	/// <returns>A <see cref="Span{T}"/> over the bytes written to the stream.</returns>
	/// <inheritdoc cref="EnsureNotDisposed" path="/exception"/>
	public Span<byte> AsSpan()
	{
		this.EnsureNotDisposed();
		return _buffer.AsSpan(0, _length);
	}
	/// <summary>
	/// Returns a read-only span over the populated contents of the stream, starting at the specified index and with the specified length.
	/// </summary>
	/// <param name="start">The starting index of the span.</param>
	/// <param name="length">The length of the span.</param>
	/// <returns>A <see cref="ReadOnlySpan{T}"/> over the specified range of bytes written to the stream.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> or <paramref name="length"/> is negative, or when the range defined by <paramref name="start"/> and <paramref name="length"/> exceeds the length of the stream.</exception>
	/// <inheritdoc cref="EnsureNotDisposed" path="/exception"/>
	[DebuggerStepThrough]
	internal ReadOnlySpan<byte> AsSpan(int start, int length)
	{
		this.EnsureNotDisposed();
		ValidateContentsRange(start, length, _length);
		return new ReadOnlySpan<byte>(_buffer, start, length);
	}
	/// <summary>
	/// Returns the populated contents of the stream as a read-only memory region.
	/// </summary>
	/// <returns>A <see cref="ReadOnlyMemory{T}"/> over the bytes written to the stream.</returns>
	/// <inheritdoc cref="AsMemory(int, int)" path="/exception"/>
	[DebuggerStepThrough]
	public ReadOnlyMemory<byte> AsMemory()
	{
		return this.AsMemory(0, _length);
	}
	/// <summary>
	/// Returns a read-only memory region over the populated contents of the stream, starting at the specified index and with the specified length.
	/// </summary>
	/// <param name="start">The starting index of the memory region.</param>
	/// <param name="length">The length of the memory region.</param>
	/// <returns>A <see cref="ReadOnlyMemory{T}"/> over the specified range of bytes written to the stream.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> or <paramref name="length"/> is negative, or when the range defined by <paramref name="start"/> and <paramref name="length"/> exceeds the length of the stream.</exception>
	/// <inheritdoc cref="EnsureNotDisposed" path="/exception"/>
	[DebuggerStepThrough]
	public ReadOnlyMemory<byte> AsMemory(int start, int length)
	{
		this.EnsureNotDisposed();
		ValidateContentsRange(start, length, _length);
		return new ReadOnlyMemory<byte>(_buffer, start, length);
	}

	/// <inheritdoc/>
	[DebuggerStepThrough]
	public override void Flush() { /* no-op */ }

	/// <summary>
	/// Determines whether the start of the stream's contents matches the specified byte sequence.
	/// </summary>
	/// <param name="other">The byte sequence to compare against the beginning of the stream.</param>
	/// <returns><see langword="true"/> if the stream holds at least as many bytes as <paramref name="other"/> and its leading bytes equal <paramref name="other"/>; otherwise, <see langword="false"/>.</returns>
	public bool IsSequenceEqual(ReadOnlySpan<byte> other)
	{
		return _buffer is not null && _length >= other.Length && other.SequenceEqual(_buffer.AsSpan(0, other.Length));
	}

	/// <inheritdoc/>
	public override int Read(byte[] buffer, int offset, int count)
	{
		ValidateReadArgs(buffer, offset, count);
		this.EnsureNotDisposed();
		int remaining = Math.Min(count, _length - _position);
		if (remaining <= 0)
			return 0;

		Buffer.BlockCopy(_buffer, _position, buffer, offset, remaining);
		_position += remaining;
		return remaining;
	}
	/// <inheritdoc/>
	public override int Read(Span<byte> destination)
	{
		this.EnsureNotDisposed();
		int read = Math.Min(destination.Length, _length - _position);
		if (read <= 0)
			return 0;

		new ReadOnlySpan<byte>(_buffer, _position, read).CopyTo(destination);
		_position += read;
		return read;
	}

	/// <inheritdoc/>
	public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
	{
		if (IsTokenCancelled<int>(cancellationToken, out var cancelled))
		{
			return cancelled;
		}

		try
		{
			int read = this.Read(destination.Span);
			return ValueTask.FromResult(read);
		}
		catch (Exception ex)
		{
			return ValueTask.FromException<int>(ex);
		}
	}

	/// <inheritdoc/>
	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return this.ReadAsync(destination: buffer.AsMemory(offset, count), cancellationToken)
				   .AsTask();
	}

	internal string ReadString(Encoding encoding)
	{
		unsafe
		{
			// SAFETY: Every write and SetLength makes room in _buffer before it raises _length, so _length never exceeds
			// _buffer.Length while no other thread uses the stream.
			return _length > 0 && _buffer is not null
				? encoding.GetString(_buffer.AsUnsafeSpan(0, _length))
				: string.Empty;
		}
	}

	/// <summary>
	/// Resets the read/write position to the beginning of the stream without discarding its contents.
	/// </summary>
	/// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
	[DebuggerStepThrough]
	public void Rewind()
	{
		this.EnsureNotDisposed();
		_position = 0;
	}

	/// <inheritdoc/>
	public override long Seek(long offset, SeekOrigin origin)
	{
		this.EnsureNotDisposed();
		long start = origin switch
		{
			SeekOrigin.Begin => 0,
			SeekOrigin.Current => _position,
			SeekOrigin.End => _length,
			_ => throw new ArgumentException("The seek origin isn't a SeekOrigin value.", nameof(origin))
		};

		if (offset > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException(nameof(offset), "The offset must be less than 2^31-1.");
		}

		long newPos = start + offset;

		return _position = newPos switch
		{
			< 0 => throw new IOException("Attempted to seek before beginning of stream."),
			> int.MaxValue => throw new ArgumentOutOfRangeException(nameof(offset), "Resulting stream position must be less than 2^31-1."),
			_ => (int)newPos,
		};
	}

	/// <inheritdoc/>
	public override void SetLength(long value)
	{
		this.EnsureNotDisposed();
		if (value < 0 || value > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException(nameof(value));
		}

		int newLength = (int)value;
		this.EnsureCapacity(newLength);
		if (newLength > _length)
		{
			// Zero newly exposed bytes per MemoryStream semantics
			Array.Clear(_buffer!, _length, newLength - _length);
		}

		_length = newLength;
		if (_position > _length)
		{
			_position = _length;
		}
	}

	/// <inheritdoc/>
	public override void Write(byte[] buffer, int offset, int count)
	{
		ValidateWriteArgs(buffer, offset, count);
		this.EnsureNotDisposed();
		this.WriteCore(buffer.AsSpan(offset, count));
	}

	/// <inheritdoc/>
	public override void Write(ReadOnlySpan<byte> source)
	{
		this.EnsureNotDisposed();
		this.WriteCore(source);
	}

	/// <inheritdoc/>
	public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
	{
		if (IsTokenCancelled(cancellationToken, out var cancelled))
		{
			return cancelled;
		}

		try
		{
			this.Write(source.Span);
			return ValueTask.CompletedTask;
		}
		catch (Exception ex)
		{
			return ValueTask.FromException(ex);
		}
	}

	/// <inheritdoc/>
	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return this.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
	}

	/// <inheritdoc/>
	public override int ReadByte()
	{
		this.EnsureNotDisposed();
		int b = -1;
		if (_position < _length)
		{
			b = _buffer[_position++];
		}

		return b;
	}

	/// <inheritdoc/>
	/// <remarks>
	/// A write at a position of <see cref="int.MaxValue"/> throws an <see cref="IOException"/>, since the stream can't
	/// grow past it, where <see cref="MemoryStream.WriteByte(byte)"/> overflows its position and throws an
	/// <see cref="IndexOutOfRangeException"/>.
	/// </remarks>
	public override void WriteByte(byte value)
	{
		this.EnsureNotDisposed();
		this.EnsureCapacityForAppend(1);
		this.ClearGapBeforePosition();
		_buffer[_position++] = value;
		if (_position > _length)
		{
			_length = _position;
		}
	}

	/// <summary>
	/// Releases the resources used by the <see cref="ArrayPoolMemoryStream"/> and returns the rented buffer to the pool.
	/// </summary>
	/// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			this.ReturnBufferOnce();
			_length = 0;
			_position = 0;
		}

		this.Disposed = null; // release references to any subscribers
		base.Dispose(disposing);
	}

	private void WriteCore(ReadOnlySpan<byte> source)
	{
		this.EnsureCapacityForAppend(source.Length);
		this.ClearGapBeforePosition();
		source.CopyTo(new Span<byte>(_buffer, _position, source.Length));
		_position += source.Length;
		if (_position > _length)
		{
			_length = _position;
		}
	}

	/// <summary>
	/// Zeroes the bytes between the end of the stream's contents and the position, when a seek has moved the position
	/// past the end, so that a write there doesn't expose bytes that the pooled buffer held before it was rented.
	/// </summary>
	/// <remarks>
	/// <see cref="MemoryStream"/> zeroes the same gap, as <see cref="SetLength(long)"/> does for the bytes it adds. Call
	/// it after <see cref="EnsureCapacityForAppend(int)"/>, which makes the buffer reach the position.
	/// </remarks>
	private void ClearGapBeforePosition()
	{
		if (_position > _length)
		{
			Array.Clear(_buffer!, _length, _position - _length);
		}
	}

	private void EnsureCapacityForAppend(int appendCount)
	{
		long required = (long)_position + appendCount;
		if (required > int.MaxValue)
		{
			throw new IOException("Stream too large.");
		}

		this.EnsureCapacity((int)required);
	}

	/// <summary>
	/// Ensures capacity is at least <paramref name="minCapacity"/>. If expansion is needed,
	/// rents a new buffer, copies data, and returns the old buffer to the pool.
	/// </summary>
	private void EnsureCapacity(int minCapacity)
	{
		Debug.Assert(minCapacity >= 0);
		byte[]? buf = _buffer;
		ObjectDisposedException.ThrowIf(buf is null, this);

		if (buf.Length >= minCapacity) return;

		// Growth policy: double until sufficient, capping at int.MaxValue
		int newCapacity = buf.Length;

		if (newCapacity == 0) newCapacity = 1;

		while (newCapacity < minCapacity)
		{
			int next = newCapacity << 1;
			if (next <= 0 || next > int.MaxValue)
			{
				newCapacity = minCapacity;
				break;
			}

			newCapacity = next;
		}

		byte[] newBuf = _pool.Rent(newCapacity);
		if (_length > 0)
		{
			Buffer.BlockCopy(buf, 0, newBuf, 0, _length);
		}

		// Return old buffer immediately
		if (this.ClearOnReturn)
		{
			Array.Clear(buf, 0, Math.Min(buf.Length, _length));
		}

		_pool.Return(buf, this.ClearOnReturn);

		_buffer = newBuf;
	}

	/// <summary>
	/// Returns the rented buffer to the pool exactly once, racing disposers included.
	/// </summary>
	/// <remarks>
	/// <para>The buffer reference is nulled before <see cref="Disposed"/> fires and the array
	/// re-enters the pool, so a racing reader fails fast on the disposed stream instead of
	/// observing recycled bytes.</para>
	/// </remarks>
	private void ReturnBufferOnce()
	{
		byte[]? buf = Interlocked.Exchange(ref _buffer, null);
		if (buf is null)
			return;

		this.Disposed?.Invoke();

		if (this.ClearOnReturn)
		{
			Array.Clear(buf, 0, Math.Min(buf.Length, _length));
		}

		_pool.Return(buf, this.ClearOnReturn);
	}

	/// <summary>
	/// Throws <see cref="ObjectDisposedException"/> if the stream has been disposed, ensuring that the underlying buffer is still valid.
	/// </summary>
	/// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
	[MemberNotNull(nameof(_buffer)), DebuggerStepThrough]
	private void EnsureNotDisposed()
	{
		ObjectDisposedException.ThrowIf(_buffer is null, this);
	}

	/// <summary>
	/// Checks if the provided <see cref="CancellationToken"/> has been canceled and returns a canceled <see cref="ValueTask"/> if it has.
	/// </summary>
	/// <param name="token">The cancellation token to check.</param>
	/// <param name="cancelledResult">The canceled <see cref="ValueTask"/> if the token is canceled.</param>
	/// <returns>True if the token is canceled; otherwise, false.</returns>
	[DebuggerStepThrough]
	private static bool IsTokenCancelled(CancellationToken token, out ValueTask cancelledResult)
	{
		if (token.IsCancellationRequested)
		{
			cancelledResult = ValueTask.FromCanceled(token);
			return true;
		}

		cancelledResult = default;
		return false;
	}
	/// <summary>
	/// Checks if the provided <see cref="CancellationToken"/> has been canceled and returns a canceled <see cref="ValueTask{T}"/> if it has.
	/// </summary>
	/// <typeparam name="T">The type of the result.</typeparam>
	/// <param name="token">The cancellation token to check.</param>
	/// <param name="cancelledResult">The canceled <see cref="ValueTask{T}"/> if the token is canceled.</param>
	/// <returns>True if the token is canceled; otherwise, false.</returns>
	[DebuggerStepThrough]
	private static bool IsTokenCancelled<T>(CancellationToken token, out ValueTask<T> cancelledResult)
	{
		if (token.IsCancellationRequested)
		{
			cancelledResult = ValueTask.FromCanceled<T>(token);
			return true;
		}

		cancelledResult = default;
		return false;
	}

	/// <summary>
	/// Throws an <see cref="ArgumentOutOfRangeException"/> when a range doesn't lie within the stream's contents.
	/// </summary>
	/// <param name="start">The index that the range starts at.</param>
	/// <param name="length">The number of bytes in the range.</param>
	/// <param name="contentLength">The stream's length.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="start"/> or <paramref name="length"/> is negative, or when the range ends past <paramref name="contentLength"/>.</exception>
	[DebuggerStepThrough]
	private static void ValidateContentsRange(int start, int length, int contentLength)
	{
		ThrowHelper.ThrowIfNegativeOrGreaterThan(start, (uint)contentLength);
		ThrowHelper.ThrowIfNegativeOrGreaterThan(length, (uint)(contentLength - start));
	}
	[DebuggerStepThrough]
	private static void ValidateReadArgs(byte[] buffer, int offset, int count)
	{
		ArgumentNullException.ThrowIfNull(buffer);
		ThrowHelper.ThrowIfNegativeOrGreaterThan(offset, (uint)buffer.Length);
		ThrowHelper.ThrowIfNegativeOrGreaterThan(count, (uint)(buffer.Length - offset));
	}
	[DebuggerStepThrough]
	private static void ValidateWriteArgs(byte[] buffer, int offset, int count)
	{
		ArgumentNullException.ThrowIfNull(buffer);
		ThrowHelper.ThrowIfNegativeOrGreaterThan(offset, (uint)buffer.Length);
		ThrowHelper.ThrowIfNegativeOrGreaterThan(count, (uint)(buffer.Length - offset));
	}
}

