using System.Buffers;

namespace MG.Sonarr.Next.Services.Http.IO
{
    public sealed class ArrayPoolMemoryStream : Stream
    {
        private const int DEFAULT_INITIAL_CAPACITY = 8192;

        private readonly ArrayPool<byte> _pool;
        private byte[]? _buffer;              // null => disposed
        private long _length;
        private long _position;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity in bytes; rounded up to at least 1.</param>
        /// <param name="pool">Pool to rent from; defaults to <see cref="ArrayPool{T}.Shared"/>.</param>
        public ArrayPoolMemoryStream(int initialCapacity = DEFAULT_INITIAL_CAPACITY, ArrayPool<byte>? pool = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);

            _pool = pool ?? ArrayPool<byte>.Shared;
            _buffer = _pool.Rent(Math.Max(1, initialCapacity));
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
                ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(Position));
                _position = value;
            }
        }

        /// <summary>
        /// Returns the stream contents as a new array of length <see cref="Length"/>.
        /// </summary>
        public byte[] ToArray()
        {
            this.EnsureNotDisposed();
            if (_length == 0)
                return [];

            byte[] result = new byte[checked((int)_length)];
            Buffer.BlockCopy(_buffer, 0, result, 0, checked((int)_length));

            return result;
        }

        /// <summary>
        /// Returns an <see cref="ArraySegment{T}"/> over the live pooled buffer for the readable portion [0, <see cref="Length"/>).
        /// Use with care; the segment is invalid after dispose or any expansion.
        /// </summary>
        public bool TryGetBuffer(out ArraySegment<byte> segment)
        {
            if (_buffer is null)
            {
                segment = [];
                return false;
            }

            segment = new ArraySegment<byte>(_buffer, 0, checked((int)_length));
            return true;
        }

        /// <inheritdoc/>
        public override void Flush() { /* no-op */ }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateReadArgs(buffer, offset, count);
            this.EnsureNotDisposed();
            int remaining = (int)Math.Min(count, _length - _position);
            if (remaining <= 0)
                return 0;

            Buffer.BlockCopy(_buffer, (int)_position, buffer, offset, remaining);
            _position += remaining;
            return remaining;
        }

        /// <inheritdoc/>
        public override int Read(Span<byte> destination)
        {
            this.EnsureNotDisposed();
            int remaining = (int)Math.Min(destination.Length, _length - _position);
            if (remaining <= 0)
                return 0;

            new Span<byte>(_buffer, (int)_position, remaining).CopyTo(destination);
            _position += remaining;
            return remaining;
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
            return this.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        internal void Rewind()
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
            if (newPos < 0)
                throw new IOException("Attempted to seek before beginning of stream.");

            _position = newPos;
            return _position;
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            this.EnsureNotDisposed();
            if (value < 0 || value > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value));

            this.EnsureCapacity((int)value);
            if (value > _length)
            {
                // Zero newly exposed bytes per MemoryStream semantics
                int start = checked((int)_length);
                int len = checked((int)(value - _length));
                Array.Clear(_buffer!, start, len);
            }

            _length = value;
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
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            this.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        /// <inheritdoc/>
        public override int ReadByte()
        {
            this.EnsureNotDisposed();
            if (_position >= _length)
                return -1;
            int b = _buffer![_position];
            _position++;
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.ReturnAndNullBuffer();
                _length = 0;
                _position = 0;
            }

            base.Dispose(disposing);
        }

        private void WriteCore(ReadOnlySpan<byte> source)
        {
            this.EnsureCapacityForAppend(source.Length);
            source.CopyTo(new Span<byte>(_buffer!, (int)_position, source.Length));
            _position += source.Length;
            if (_position > _length)
                _length = _position;
        }

        private void EnsureCapacityForAppend(int appendCount)
        {
            long required = _position + appendCount;
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
                Buffer.BlockCopy(buf, 0, newBuf, 0, checked((int)_length));

            // Return old buffer immediately
            if (this.ClearOnReturn)
                Array.Clear(buf, 0, Math.Min(buf.Length, checked((int)_length)));

            _pool.Return(buf, this.ClearOnReturn);

            _buffer = newBuf;
        }

        private void ReturnAndNullBuffer()
        {
            byte[]? buf = _buffer;
            if (buf is null)
                return;

            if (this.ClearOnReturn)
                Array.Clear(buf, 0, Math.Min(buf.Length, checked((int)_length)));

            _pool.Return(buf, this.ClearOnReturn);
            _buffer = null;
        }

        [MemberNotNull(nameof(_buffer))]
        private void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_buffer is null, this);
        }

        private static void ValidateReadArgs(byte[] buffer, int offset, int count)
        {
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));

            if ((uint)offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if ((uint)count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
        }

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
}

