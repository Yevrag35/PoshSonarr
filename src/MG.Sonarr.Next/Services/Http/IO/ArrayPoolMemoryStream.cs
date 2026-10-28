using MG.Sonarr.Next.Buffers;
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
/// </remarks>
[DebuggerDisplay(@"\{Length = {Length}, Position = {Position}, Capacity = {Capacity}\}"), DebuggerStepThrough]
public sealed class ArrayPoolMemoryStream : Stream
{
	private const int DEFAULT_INITIAL_CAPACITY = 8192;
	private const int RETIRED_FLAG = 1 << 30;
	private const int VIEW_COUNT_MASK = RETIRED_FLAG - 1;

	private readonly ArrayPool<byte> _pool;
	private byte[]? _buffer;              // null => disposed
	private int _length;
	private int _position;
	private int _viewState;               // RETIRED_FLAG bit | active view count

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
	/// Gets the number of active views that hold a lease on the stream's buffer.
	/// </summary>
	/// <remarks>
	/// No member of the stream creates a view, so the count stays at zero.
	/// </remarks>
	/// <value>The number of views that haven't been disposed.</value>
	public int ActiveViewCount => Volatile.Read(ref _viewState) & VIEW_COUNT_MASK;

	/// <summary>
	/// The current capacity of the underlying rented buffer.
	/// </summary>
	public int Capacity => _buffer?.Length ?? 0;

	/// <summary>
	/// Gets a value that indicates whether the stream is retired, which closes it to new views and returns its buffer to
	/// the pool once the last view is disposed.
	/// </summary>
	/// <remarks>
	/// No member of the stream retires it, so the value stays <see langword="false"/>.
	/// </remarks>
	/// <value><see langword="true"/> if the stream is retired; otherwise, <see langword="false"/>.</value>
	public bool IsRetired => (Volatile.Read(ref _viewState) & RETIRED_FLAG) != 0;

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
		get => _position;
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
	public bool TryGetBuffer(out ArraySlice<byte> segment)
	{
		if (_buffer is not null)
		{
			segment = ArraySliceMarshal.Create(_buffer, 0, _length);
			return true;
		}

		segment = [];
		return false;
	}

	///// <summary>
	///// Creates a read-only, independently-seekable view over this stream's buffer.
	///// </summary>
	///// <remarks>
	///// <para>Each view has its own <see cref="Stream.Position"/> so multiple consumers can
	///// read from the same data without interfering with each other.</para>
	///// <para>Each view holds a lease on the pooled buffer: after <see cref="Retire"/> is called,
	///// the buffer is returned to the pool only once every view has been disposed. Do not create
	///// views over a stream that is still being written - growth would swap the buffer out from
	///// under them.</para>
	///// <para><b>Threading:</b> View creation, view disposal, <see cref="Retire"/>, and
	///// <see cref="Stream.Dispose()"/> are thread-safe relative to each other.</para>
	///// </remarks>
	///// <returns>A new <see cref="Stream"/> that reads from the same buffer with an independent position.</returns>
	///// <exception cref="ObjectDisposedException">Thrown when the stream is disposed or retired.</exception>
	//public Stream CreateView()
	//{
	//	return this.CreateView(companion: null);
	//}

	///// <summary>
	///// Creates a read-only, independently-seekable view over this stream's buffer that disposes the specified companion along with itself.
	///// </summary>
	///// <remarks>
	///// <para>The view takes ownership of <paramref name="companion"/> and disposes it exactly once, when the view itself is first disposed.
	///// That holds even when a <see cref="Stream.Dispose()"/> of this stream has already invalidated the view. Use the companion for whatever
	///// must last exactly as long as the view, such as a cache lease that keeps this stream from being retired while the view reads it.</para>
	///// <para>Ownership passes only when a view is created. When this method throws, the caller still owns <paramref name="companion"/>.</para>
	///// <para>See <see cref="CreateView()"/> for the view's lease on the pooled buffer and the threading guarantees.</para>
	///// </remarks>
	///// <param name="companion">The object the view disposes when it is disposed, or <see langword="null"/> for none.</param>
	///// <returns>A new <see cref="Stream"/> that reads from the same buffer with an independent position.</returns>
	///// <exception cref="ObjectDisposedException">Thrown when the stream is disposed or retired.</exception>
	//public Stream CreateView(IDisposable? companion)
	//{
	//	ObjectDisposedException.ThrowIf(!this.TryCreateView(companion, out Stream? view), this);

	//	return view;
	//}

	///// <summary>
	///// Attempts to create a read-only, independently-seekable view over this stream's buffer.
	///// </summary>
	///// <remarks>
	///// <para>This is the non-throwing counterpart of <see cref="CreateView()"/> for callers racing
	///// against <see cref="Retire"/> (e.g., a cache entry being evicted concurrently); see that
	///// method for the lease semantics.</para>
	///// </remarks>
	///// <param name="view">When this method returns <see langword="true"/>, contains the new view; otherwise, <see langword="null"/>.</param>
	///// <returns><see langword="true"/> when a view was created; <see langword="false"/> when the stream is disposed or retired.</returns>
	//public bool TryCreateView([NotNullWhen(true)] out Stream? view)
	//{
	//	return this.TryCreateView(companion: null, out view);
	//}

	///// <summary>
	///// Attempts to create a read-only, independently-seekable view over this stream's buffer that disposes the specified companion along
	///// with itself.
	///// </summary>
	///// <remarks>
	///// <para>This is the non-throwing counterpart of <see cref="CreateView(IDisposable)"/>; see that method for how the view owns
	///// <paramref name="companion"/>. When this method returns <see langword="false"/>, no view took ownership, and the caller still owns
	///// <paramref name="companion"/>.</para>
	///// </remarks>
	///// <param name="companion">The object the view disposes when it is disposed, or <see langword="null"/> for none.</param>
	///// <param name="view">When this method returns <see langword="true"/>, contains the new view; otherwise, <see langword="null"/>.</param>
	///// <returns><see langword="true"/> when a view was created; <see langword="false"/> when the stream is disposed or retired.</returns>
	//public bool TryCreateView(IDisposable? companion, [NotNullWhen(true)] out Stream? view)
	//{
	//	if (!this.TryAcquireViewLease())
	//	{
	//		view = null;
	//		return false;
	//	}

	//	ArrayPoolMemoryStreamView typedView = new(this, companion);
	//	this.Disposed += typedView.OnSourceDisposed;
	//	view = typedView;
	//	return true;
	//}

	///// <summary>
	///// Atomically takes one view lease, failing when the stream is already retired or disposed.
	///// </summary>
	///// <returns><see langword="true"/> when the active view count was incremented; otherwise, <see langword="false"/>.</returns>
	//private bool TryAcquireViewLease()
	//{
	//	int state = Volatile.Read(ref _viewState);
	//	while ((state & RETIRED_FLAG) == 0 && _buffer is not null)
	//	{
	//		int witnessed = Interlocked.CompareExchange(ref _viewState, state + 1, state);
	//		if (witnessed == state)
	//			return true;

	//		state = witnessed;
	//	}

	//	return false;
	//}

	///// <summary>
	///// Marks the stream as retired: no new views can be created, and the pooled buffer is returned
	///// once the last active view is disposed - immediately when no views exist.
	///// </summary>
	///// <remarks>
	///// <para>Use this when ownership rests with a component (such as a cache) that can revoke the
	///// entry while readers may still hold views. Unlike <see cref="Stream.Dispose()"/>, retiring
	///// never invalidates an active view, so a reader mid-stream is unaffected.</para>
	///// <para>This method is idempotent and thread-safe.</para>
	///// </remarks>
	//public void Retire()
	//{
	//	int state = Volatile.Read(ref _viewState);
	//	while (true)
	//	{
	//		if ((state & RETIRED_FLAG) != 0)
	//			return;

	//		int witnessed = Interlocked.CompareExchange(ref _viewState, state | RETIRED_FLAG, state);
	//		if (witnessed == state)
	//			break;

	//		state = witnessed;
	//	}

	//	if ((state & VIEW_COUNT_MASK) == 0)
	//		this.Dispose();
	//}

	///// <summary>
	///// Releases one view lease, disposing the stream when it is retired and this was the last lease.
	///// </summary>
	//internal void ReleaseView()
	//{
	//	int state = Volatile.Read(ref _viewState);
	//	while (true)
	//	{
	//		Debug.Assert((state & VIEW_COUNT_MASK) != 0, "Unbalanced view lease release.");
	//		if ((state & VIEW_COUNT_MASK) == 0)
	//			return;

	//		int newState = state - 1;
	//		int witnessed = Interlocked.CompareExchange(ref _viewState, newState, state);
	//		if (witnessed == state)
	//		{
	//			if (newState == RETIRED_FLAG)
	//				this.Dispose();

	//			return;
	//		}

	//		state = witnessed;
	//	}
	//}

	/// <summary>
	/// Returns the populated contents of the stream as a span over the underlying buffer.
	/// </summary>
	/// <remarks>
	/// It is not supported to write to the returned span concurrently with writing to the stream itself, as that may cause the buffer to be reallocated and invalidate the span.
	/// <para>
	/// Only use this method to populate the stream's underlying buffer when using the <see cref="ArrayPoolMemoryStream"/> as a passed, read-only stream.
	/// </para>
	/// </remarks>
	/// <returns>A <see cref="Span{T}"/> over the bytes written to the stream.</returns>
	/// <inheritdoc cref="EnsureNotDisposed" path="/exception"/>
	public Span<byte> AsSpan()
	{
		this.EnsureNotDisposed();
		return _buffer.AsSpan();
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
		if (cancellationToken.IsCancellationRequested)
			return ValueTask.FromCanceled<int>(cancellationToken);

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
		return _length > 0 && _buffer is not null
			? encoding.GetString(_buffer.AsUnsafeSpan(0, _length))
			: string.Empty;
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
		long newPos = origin switch
		{
			SeekOrigin.Begin => offset,
			SeekOrigin.Current => _position + offset,
			SeekOrigin.End => _length + offset,
			_ => throw new ArgumentOutOfRangeException(nameof(origin))
		};

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
			throw new ArgumentOutOfRangeException(nameof(value));

		int newLength = (int)value;
		this.EnsureCapacity(newLength);
		if (newLength > _length)
		{
			// Zero newly exposed bytes per MemoryStream semantics
			Array.Clear(_buffer!, _length, newLength - _length);
		}

		_length = newLength;
		if (_position > _length)
			_position = _length;
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
		if (cancellationToken.IsCancellationRequested)
			return ValueTask.FromCanceled(cancellationToken);

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
			b = _buffer![_position];
			_position++;
		}

		return b;
	}

	/// <inheritdoc/>
	public override void WriteByte(byte value)
	{
		this.EnsureNotDisposed();
		this.EnsureCapacityForAppend(1);
		_buffer![_position] = value;
		_position++;
		if (_position > _length)
			_length = _position;
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
		source.CopyTo(new Span<byte>(_buffer!, _position, source.Length));
		_position += source.Length;
		if (_position > _length)
			_length = _position;
	}

	private void EnsureCapacityForAppend(int appendCount)
	{
		long required = (long)_position + appendCount;
		if (required > int.MaxValue)
			throw new IOException("Stream too large.");
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

		if (buf.Length >= minCapacity)
			return;

		Debug.Assert((Volatile.Read(ref _viewState) & VIEW_COUNT_MASK) == 0,
			"Growing the stream would return a buffer that active views still read.");

		// Growth policy: double until sufficient, capping at int.MaxValue
		int newCapacity = buf.Length;

		if (newCapacity == 0)
			newCapacity = 1;

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
			Buffer.BlockCopy(buf, 0, newBuf, 0, _length);

		// Return old buffer immediately
		if (this.ClearOnReturn)
			Array.Clear(buf, 0, Math.Min(buf.Length, _length));

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
			Array.Clear(buf, 0, Math.Min(buf.Length, _length));

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

	[DebuggerStepThrough]
	private static void ValidateReadArgs(byte[] buffer, int offset, int count)
	{
		ArgumentNullException.ThrowIfNull(buffer);

		if ((uint)offset > buffer.Length)
			throw new ArgumentOutOfRangeException(nameof(offset));

		if ((uint)count > buffer.Length - offset)
			throw new ArgumentOutOfRangeException(nameof(count));
	}
	[DebuggerStepThrough]
	private static void ValidateWriteArgs(byte[] buffer, int offset, int count)
	{
		if (buffer is null)
			throw new ArgumentNullException(nameof(buffer));

		if ((uint)offset > buffer.Length)
			throw new ArgumentOutOfRangeException(nameof(offset));

		if ((uint)count > buffer.Length - offset)
			throw new ArgumentOutOfRangeException(nameof(count));
	}
}

