using MG.Sonarr.Next.Buffers.Intrinsics;

namespace MG.Sonarr.Next.Buffers;

/// <summary>
/// Represents a pooled buffer of type <typeparamref name="T"/>, providing access to its underlying array and handling disposal to return it to the pool.
/// </summary>
/// <remarks>
/// Unlike <see cref="RentedBuffer{T}"/>, this is a regular (non-<see langword="ref"/>) value type, so instances may be stored as fields or used across
/// <see langword="await"/> boundaries. This convenience comes with trade-offs: the API surface is deliberately minimal (no resizing, no span-wrapping constructors)
/// and, because the struct is mutable, copying an instance and disposing the copy leaves the original pointing at an already-returned array.
/// Prefer passing by <see langword="ref"/>, or using the <c>using</c> statement directly on the constructed instance to keep lifetime clear.
/// </remarks>
/// <typeparam name="T">The type of elements in the buffer.</typeparam>
[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay("IsRented = {IsRented}, Length = {_length}, Capacity = {Capacity}")]
public struct PooledBuffer<T> : IDisposable
{
	private const byte RENTED_BIT = 0x1;
	private const byte CLEAR_ON_DISPOSE_BIT = 0x2;
	private const string EMPTY_TOSTRING = "{IsEmpty = true}";

	private T[]? _array;
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private int _length;
	private byte _flags;

	/// <summary>
	/// Gets a reference to the element at the specified index in the buffer.
	/// </summary>
	/// <remarks>The index must be within the bounds of <see cref="Length"/>, not <see cref="Capacity"/>.</remarks>
	/// <param name="index">The zero-based index of the element to get.</param>
	/// <returns>A reference to the element of type <typeparamref name="T"/> at the specified index.</returns>
	/// <exception cref="NullReferenceException"/>
	public readonly ref T this[int index]
	{
		get
		{
			return ref (uint)index < (uint)_length && (uint)index < (uint)_array!.Length
				? ref _array.Ref(index)
				: ref Unsafe.NullRef<T>();
		}
	}

	/// <summary>
	/// Gets the number of elements the buffer can store.
	/// </summary>
	public readonly int Capacity => _array?.Length ?? 0;
    /// <summary>
    /// Gets or sets whether the buffer should be cleared if it was rented from the <see cref="ArrayPool{T}"/> when this instance is disposed.
    /// </summary>
    /// <remarks>
    /// If <typeparamref name="T"/> is a reference type or contains references, this property does <strong>not</strong> need to be set
    /// to <see langword="true"/> as those types are cleared regardless of this value.
    /// </remarks>
    public bool ClearOnDispose
	{
		readonly get => (_flags & CLEAR_ON_DISPOSE_BIT) != 0;
		set => _flags = value
			? (byte)(_flags | CLEAR_ON_DISPOSE_BIT)
			: (byte)(_flags & ~CLEAR_ON_DISPOSE_BIT);
	}

	/// <summary>
	/// Indicates whether this <see cref="PooledBuffer{T}"/> instance is default-initialized (i.e., has no underlying array).
	/// </summary>
	public readonly bool IsDefault
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _array is null;
	}
	/// <summary>
	/// Indicates whether this <see cref="PooledBuffer{T}"/> instance is empty or default-initialized.
	/// </summary>
	public readonly bool IsDefaultOrEmpty
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _length == 0;
	}
	/// <summary>
	/// Gets a value indicating whether the buffer is rented from the pool.
	/// </summary>
	[MemberNotNullWhen(true, nameof(_array))]
	public readonly bool IsRented => (_flags & RENTED_BIT) != 0;
	/// <summary>
	/// Gets the logical length of the buffer.
	/// </summary>
	/// <remarks>This value will always be less than or equal to <see cref="Capacity"/>.</remarks>
	public readonly int Length => _length;
	/// <summary>
	/// Gets the number of elements in the buffer.
	/// </summary>
	/// <remarks>Equivalent to <see cref="Length"/>; provided for collection-style consumers.</remarks>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public readonly int Count => _length;

	/// <summary>
	/// Initializes a new instance of the <see cref="PooledBuffer{T}"/> with the specified minimum length.
	/// </summary>
	/// <param name="minimumLength">The minimum length of the buffer to rent.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minimumLength"/> is negative.</exception>
	[DebuggerStepThrough]
	public PooledBuffer(int minimumLength)
		: this(minimumLength: minimumLength, useEntireCapacity: false)
	{
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="PooledBuffer{T}"/> with the specified minimum length and a flag indicating
	/// whether <see cref="Length"/> should report the full pool-returned array length or just <paramref name="minimumLength"/>.
	/// </summary>
	/// <param name="minimumLength">The minimum length of the buffer to rent.</param>
	/// <param name="useEntireCapacity">
	/// If <see langword="true"/>, <see cref="Length"/> matches <see cref="Capacity"/>; otherwise, <see cref="Length"/> is <paramref name="minimumLength"/>.
	/// </param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minimumLength"/> is negative.</exception>
	public PooledBuffer(int minimumLength, bool useEntireCapacity)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(minimumLength);
		if (minimumLength == 0)
		{
			this = default;
			return;
		}

		T[] array = Rent.Array<T>(minimumLength);
		_array = array;
		_length = useEntireCapacity ? array.Length : minimumLength;
		_flags = RENTED_BIT;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PooledBuffer{T}"/> with the specified minimum length without argument validation.
	/// </summary>
	/// <remarks>
	/// This overload exists for internal call sites that already validate <paramref name="minimumLength"/>.
	/// </remarks>
	/// <param name="internal">A sentinel parameter that distinguishes this constructor from public overloads.</param>
	/// <param name="minimumLength">The minimum length of the buffer to rent.</param>
	/// <param name="useEntireCapacity">
	/// <see langword="true"/> to set <see cref="Length"/> to <see cref="Capacity"/>; otherwise, set <see cref="Length"/> to <paramref name="minimumLength"/>.
	/// </param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used as a sentinel parameter to distinguish this constructor from public overloads.")]
	internal PooledBuffer(bool @internal, int minimumLength, bool useEntireCapacity)
	{
		if (minimumLength == 0)
		{
			this = default;
			return;
		}

		T[] array = Rent.Array<T>(minimumLength);
		_array = array;
		_length = useEntireCapacity ? array.Length : minimumLength;
		_flags = RENTED_BIT;
	}

	/// <summary>
	/// Returns an <see cref="ArraySlice{T}"/> that maps to the requested segment of this buffer.
	/// </summary>
	/// <param name="index">The zero-based starting index of the slice.</param>
	/// <param name="length">The desired length of the slice.</param>
	/// <returns>
	/// An <see cref="ArraySlice{T}"/> over the requested segment when the range is valid; otherwise, the default slice.
	/// </returns>
	internal readonly ArraySlice<T> AsArraySlice(int index, int length)
	{
		// The null check is not redundant with the bounds test: a zero-length rent leaves this instance default, and
		// a (0, 0) request satisfies the bounds arm while there is no array for ArraySliceMarshal.Create to wrap.
		return _array is not null && this.IsInRange(index, length)
			? ArraySliceMarshal.Create(_array, index, length)
			: default;
	}

	/// <summary>
	/// Returns a <see cref="Span{T}"/> over the logical length of the buffer.
	/// </summary>
	/// <returns>A span covering the first <see cref="Length"/> elements of the backing array, or an empty span if this instance is default or empty.</returns>
	public readonly Span<T> AsSpan()
	{
		return _array is not null && _array.Length != 0 && _length != 0
			? _array.AsUnsafeSpan(0, _length)
			: [];
	}
	/// <summary>
	/// Returns a <see cref="Span{T}"/> over a slice of the buffer.
	/// </summary>
	/// <param name="index">The zero-based starting index of the slice.</param>
	/// <param name="length">The desired length of the slice.</param>
	/// <returns>A span over <paramref name="length"/> elements of the backing array starting at <paramref name="index"/>.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative or greater than
	/// <see cref="Length"/>, or when <paramref name="length"/> is negative or greater than the number of elements from
	/// <paramref name="index"/> to the end of the buffer.</exception>
	public readonly Span<T> AsSpan(int index, int length)
	{
		this.ThrowIfOutOfRange(index, length);
		return length != 0
			? _array!.AsUnsafeSpan(index, length)
			: [];
	}
	/// <summary>
	/// Returns a <see cref="Memory{T}"/> over the logical length of the buffer.
	/// </summary>
	/// <remarks>Use this overload when an API requires <see cref="Memory{T}"/> instead of <see cref="Span{T}"/>
	/// (for example, asynchronous I/O APIs that must carry the buffer across <see langword="await"/> boundaries).</remarks>
	/// <returns>A <see cref="Memory{T}"/> covering the first <see cref="Length"/> elements of the backing array, or an empty memory if this instance is default or empty.</returns>
	public readonly Memory<T> AsMemory()
	{
		return _array is not null && _array.Length != 0 && _length != 0
			? CreateMemory(_array, 0, _length)
			: Memory<T>.Empty;
	}
	/// <summary>
	/// Returns a <see cref="Memory{T}"/> over a slice of the buffer.
	/// </summary>
	/// <param name="index">The zero-based starting index of the slice.</param>
	/// <param name="length">The desired length of the slice.</param>
	/// <returns>A <see cref="Memory{T}"/> over <paramref name="length"/> elements of the backing array starting at <paramref name="index"/>.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative or greater than
	/// <see cref="Length"/>, or when <paramref name="length"/> is negative or greater than the number of elements from
	/// <paramref name="index"/> to the end of the buffer.</exception>
	public readonly Memory<T> AsMemory(int index, int length)
	{
		this.ThrowIfOutOfRange(index, length);
		return length != 0
			? CreateMemory(_array, index, length)
			: Memory<T>.Empty;
	}
	/// <summary>
	/// Returns a <see cref="ReadOnlyMemory{T}"/> over a slice of the buffer.
	/// </summary>
	/// <param name="index">The zero-based starting index of the slice.</param>
	/// <param name="length">The desired length of the slice.</param>
	/// <returns>
	/// A <see cref="ReadOnlyMemory{T}"/> over <paramref name="length"/> elements of the
	/// backing array starting at <paramref name="index"/>.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative or greater than
	/// <see cref="Length"/>, or when <paramref name="length"/> is negative or greater than the number of elements from
	/// <paramref name="index"/> to the end of the buffer.</exception>
	public readonly ReadOnlyMemory<T> AsReadOnlyMemory(int index, int length)
	{
		this.ThrowIfOutOfRange(index, length);
		return length != 0
			? CreateReadOnlyMemory(_array, index, length)
			: ReadOnlyMemory<T>.Empty;
	}

	/// <summary>
	/// Creates a <see cref="Memory{T}"/> instance over the specified array segment.
	/// </summary>
	/// <param name="array">The array that backs the memory.</param>
	/// <param name="start">The zero-based starting index in <paramref name="array"/>.</param>
	/// <param name="length">The number of elements to include in the memory segment.</param>
	/// <returns>A <see cref="Memory{T}"/> over the specified segment.</returns>
	[UnsafeAccessor(UnsafeAccessorKind.Constructor)]
	private static extern Memory<T> CreateMemory(object? array, int start, int length);
	/// <summary>
	/// Creates a <see cref="ReadOnlyMemory{T}"/> instance over the specified array segment.
	/// </summary>
	/// <param name="array">The array that backs the memory.</param>
	/// <param name="start">The zero-based starting index in <paramref name="array"/>.</param>
	/// <param name="length">The number of elements to include in the memory segment.</param>
	/// <returns>A <see cref="ReadOnlyMemory{T}"/> over the specified segment.</returns>
	[UnsafeAccessor(UnsafeAccessorKind.Constructor)]
	private static extern ReadOnlyMemory<T> CreateReadOnlyMemory(object? array, int start, int length);

	/// <summary>
	/// Clears the contents of the buffer if it is rented and has a non-zero length.
	/// </summary>
	public readonly void Clear()
	{
		if (_array is { Length: > 0 } arr && _length > 0)
		{
			Array.Clear(arr, 0, _length);
		}
	}

	/// <summary>
	/// Disposes of the rented buffer, returning it to the pool if it was rented.
	/// </summary>
	/// <remarks>
	/// Safe to call on a default or already-disposed instance. After disposal, the struct is reset so that <see cref="IsRented"/> is <see langword="false"/>
	/// and <see cref="Length"/> is zero - but note that copies made before disposal will still observe the old (now-invalid) field values.
	/// </remarks>
	public void Dispose()
	{
		T[]? array = _array;
		bool rented = this.IsRented;
		bool clear = this.ClearOnDispose;
		this = default; // reset fields to prevent accidental use after dispose, but capture values needed for return beforehand
		_array = null;
		_length = 0;
		_flags = 0;

		if (rented)
		{
			Rent.Return(array, clear);
		}
	}
	/// <summary>
	/// Returns an enumerator that iterates through the pooled buffer.
	/// </summary>
	/// <returns>An enumerator for the buffer.</returns>
	[DebuggerStepThrough]
	public readonly Enumerator GetEnumerator()
	{
		return new Enumerator(_array, _length);
	}

	/// <summary>
	/// Gets the underlying rented array backing this buffer, or <see langword="null"/> if no array is currently rented.
	/// </summary>
	/// <remarks>The returned array comes from a shared pool and may be larger than <see cref="Length"/>; do not retain or return it independently of this buffer.</remarks>
	/// <returns>The backing array, or <see langword="null"/> when the buffer holds no rented array.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly T[]? GetRawArray() => _array;

	/// <summary>
	/// Grows a buffer whose logical length is less than <paramref name="newSize"/>, so that it holds at least that many
	/// elements and its logical length covers its whole backing array.
	/// </summary>
	/// <remarks>
	/// <para>A buffer whose <see cref="Length"/> already covers <paramref name="newSize"/> doesn't change. One whose
	/// backing array can hold <paramref name="newSize"/> elements keeps that array, and its <see cref="Length"/> grows to
	/// <see cref="Capacity"/>. Any other buffer rents an array of at least <paramref name="newSize"/> elements, copies
	/// its old array into it, and returns the old array to the pool.</para>
	/// <para>The buffer keeps its <see cref="ClearOnDispose"/> setting, which also decides whether the old array is
	/// cleared on its way back to the pool. The old array goes back only after the new one is rented, so a rental that
	/// throws leaves the buffer as it was.</para>
	/// </remarks>
	/// <param name="newSize">The number of elements that the buffer must be able to hold.</param>
	internal void ResizeCore(int newSize)
	{
		if (newSize <= _length)
		{
			return;
		}

		T[]? oldArray = _array;
		if (oldArray is not null && newSize <= oldArray.Length)
		{
			_length = oldArray.Length;
			return;
		}

		T[] newArray = Rent.Array<T>(newSize);
		if (oldArray is not null)
		{
			Array.Copy(oldArray, newArray, oldArray.Length);
		}

		_array = newArray;
		_length = newArray.Length;
		_flags |= RENTED_BIT;
		Rent.Return(oldArray, this.ClearOnDispose);
	}

	/// <summary>
	/// Determines whether a slice that starts at <paramref name="index"/> and covers <paramref name="length"/> elements
	/// lies within the logical length of the buffer.
	/// </summary>
	/// <remarks>
	/// Comparing as unsigned values rejects a negative argument along with one that's too large, and the subtraction
	/// can't overflow once <paramref name="index"/> is known to lie within the buffer.
	/// </remarks>
	/// <param name="index">The zero-based starting index of the slice.</param>
	/// <param name="length">The number of elements in the slice.</param>
	/// <returns><see langword="true"/> if neither value is negative and the slice ends at or before <see cref="Length"/>;
	/// otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private readonly bool IsInRange(int index, int length)
	{
		return (uint)index <= (uint)_length && (uint)length <= (uint)(_length - index);
	}

	/// <summary>
	/// Forms a slice out of the current buffer, beginning at the specified starting index and extending to the end.
	/// </summary>
	/// <param name="start">The zero-based starting index of the slice.</param>
	/// <returns>A <see cref="Span{T}"/> representing the slice.</returns>
	/// <exception cref="ArgumentOutOfRangeException"/>
	public readonly Span<T> Slice(int start)
	{
		return this.AsSpan().Slice(start);
	}
	/// <summary>
	/// Forms a slice out of the current buffer, beginning at the specified starting index and of the specified length.
	/// </summary>
	/// <param name="start">The zero-based starting index of the slice.</param>
	/// <param name="length">The desired length of the slice.</param>
	/// <returns>A <see cref="Span{T}"/> representing the slice.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="start"/> is negative or greater than
	/// <see cref="Length"/>, or when <paramref name="length"/> is negative or greater than the number of elements from
	/// <paramref name="start"/> to the end of the buffer.</exception>
	public readonly Span<T> Slice(int start, int length)
	{
		return this.AsSpan(start, length);
	}

	/// <summary>
	/// Throws an <see cref="ArgumentOutOfRangeException"/> when a slice that starts at <paramref name="index"/> and covers
	/// <paramref name="length"/> elements doesn't lie within the logical length of the buffer.
	/// </summary>
	/// <param name="index">The zero-based starting index of the slice.</param>
	/// <param name="length">The number of elements in the slice.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative or greater than
	/// <see cref="Length"/>, or when <paramref name="length"/> is negative or greater than the number of elements from
	/// <paramref name="index"/> to the end of the buffer.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private readonly void ThrowIfOutOfRange(int index, int length)
	{
		if (!this.IsInRange(index, length))
		{
			ThrowOutOfRange(index, length, _length);
		}
	}

	/// <summary>
	/// Throws an <see cref="ArgumentOutOfRangeException"/> that names the argument that puts a slice outside the buffer.
	/// </summary>
	/// <param name="index">The zero-based starting index of the slice.</param>
	/// <param name="length">The number of elements in the slice.</param>
	/// <param name="bufferLength">The logical length of the buffer.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown unconditionally.</exception>
	[DoesNotReturn, StackTraceHidden, DebuggerStepThrough, MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowOutOfRange(int index, int length, int bufferLength)
	{
		throw new ArgumentOutOfRangeException(
			paramName: (uint)index > (uint)bufferLength ? nameof(index) : nameof(length));
	}

	/// <summary>
	/// Returns a string representation of the pooled buffer.
	/// </summary>
	/// <returns>A string indicating whether the buffer is rented, its length, and its capacity.</returns>
	[DebuggerStepThrough]
	public override readonly string ToString()
	{
		return _length == 0
			? EMPTY_TOSTRING
			: $"{{IsRented = {this.IsRented}, Length = {_length}, Capacity = {this.Capacity}}}";
	}

	/// <summary>
	/// Enumerates the elements of a <see cref="PooledBuffer{T}"/>.
	/// </summary>
	[DebuggerStepThrough]
	[StructLayout(LayoutKind.Auto)]
	public struct Enumerator
	{
		private readonly T[]? _array;
		private readonly int _length;
		private int _index;

		/// <summary>
		/// Gets a reference to the current element in the collection.
		/// </summary>
		public readonly ref readonly T Current => ref _array![_index];

		/// <summary>
		/// Initializes a new <see cref="Enumerator"/> over the specified array segment.
		/// </summary>
		/// <param name="array">The backing array, or <see langword="null"/> when the source is empty.</param>
		/// <param name="length">The logical length to enumerate over.</param>
		internal Enumerator(T[]? array, int length)
		{
			_array = array;
			_length = length;
			_index = -1;
		}

		/// <summary>
		/// Advances the enumerator to the next element of the collection.
		/// </summary>
		/// <returns>
		///		<see langword="true"/> if the enumerator was successfully advanced;
		///		<see langword="false"/> if it has passed the end of the collection.
		/// </returns>
		public bool MoveNext()
		{
			int next = _index + 1;
			if ((uint)next >= (uint)_length)
			{
				_index = _length;
				return false;
			}

			_index = next;
			return true;
		}
		/// <summary>
		/// Sets the enumerator to its initial position, which is before the first element in the collection.
		/// </summary>
		public void Reset()
		{
			_index = -1;
		}
	}
}
