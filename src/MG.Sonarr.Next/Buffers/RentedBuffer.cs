namespace MG.Sonarr.Next.Buffers;

/// <summary>
/// Represents a rented buffer of type <typeparamref name="T"/>, providing access to its underlying span and handling disposal to return it to the pool.
/// </summary>
/// <typeparam name="T">The type of elements in the buffer.</typeparam>
[StructLayout(LayoutKind.Auto), DebuggerDisplay(@"\{{_span}\} IsRented = {_state.IsRented}, Length = {_length}")]
[CollectionBuilder(typeof(RentedBuffer), nameof(RentedBuffer.Create)), DebuggerStepThrough]
public ref partial struct RentedBuffer<T> : IDisposable
{
	private T[]? _array;
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private int _length;
	private State _state;
	private Span<T> _span;

	/// <summary>
	/// Gets or sets the reference of the element at the specified index in the buffer.
	/// </summary>
	/// <remarks>
	/// The index must be within the bounds of the minimum length and not the capacity of the buffer.
	/// </remarks>
	/// <param name="index">The zero-based index of the element to get.</param>
	/// <returns>A reference to the element of type <typeparamref name="T"/> at the specified index in the buffer.
	/// An invalid index will return a <see langword="null"/> reference.</returns>
	public readonly ref T this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			ref T ret = ref Unsafe.NullRef<T>();
			if ((uint)index < (uint)_length)
			{
				ret = ref _span.Ref(index);
			}

			return ref ret;
		}
	}

	/// <summary>
	/// Gets the span of elements in the rented buffer.
	/// </summary>
	public readonly Span<T> Span
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _span;
	}
	/// <summary>
	/// The number of elements that the buffer can store.
	/// </summary>
	public readonly int Capacity => _state.IsRented ? _array!.Length : _span.Length;
    /// <summary>
    /// Gets or sets whether the buffer should be cleared if the buffer has been rented from the <see cref="ArrayPool{T}"/>
    /// when this instance is disposed.
    /// </summary>
    /// <remarks>
    /// If <typeparamref name="T"/> is a reference type or contains references, this property does <strong>not</strong> need to be set
    /// to <see langword="true"/> as those types are cleared regardless of this value.
    /// </remarks>
    public bool ClearOnDispose
	{
		readonly get => _state.ClearOnDispose;
		set => _state.ClearOnDispose = value;
	}
	/// <summary>
	/// Indicates whether this <see cref="RentedBuffer{T}"/> instance is default-initialized.
	/// </summary>
	[DebuggerBrowsable(DebuggerBrowsableState.Never), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public readonly bool IsDefault => this.IsDefaultOrEmpty;
	/// <summary>
	/// Indicates whether this <see cref="RentedBuffer{T}"/> instance is empty or default-initialized.
	/// </summary>
	public readonly bool IsDefaultOrEmpty => _length == 0;
	/// <summary>
	/// Gets a value indicating whether the buffer is rented from the pool.
	/// </summary>
	[MemberNotNullWhen(true, nameof(_array))]
	public readonly bool IsRented => _state.IsRented;
	/// <summary>
	/// Gets the length of the specified minimum length.
	/// </summary>
	/// <remarks>
	/// This value will always be less than or equal to the capacity of the buffer.
	/// </remarks>
	public readonly int Length => _length;

	/// <summary>
	/// Initializes a new instance of the <see cref="RentedBuffer{T}"/> with the specified minimum length.
	/// </summary>
	/// <param name="minimumLength">The minimum length of the buffer to rent.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minimumLength"/> is negative.</exception>
	[DebuggerStepThrough]
	public RentedBuffer(int minimumLength)
		: this(minimumLength: minimumLength, preValidated: false, useEntireCapacity: false)
	{
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="RentedBuffer{T}"/> with the specified minimum length and a flag indicating whether to use the entire capacity of the buffer
	/// or just the minimum length.
	/// </summary>
	/// <param name="minimumLength">The minimum length of the buffer to rent.</param>
	/// <param name="useEntireCapacity">When <see langword="true"/>, the buffer's logical length spans the full rented array; otherwise it is limited to <paramref name="minimumLength"/>.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minimumLength"/> is negative.</exception>
	[DebuggerStepThrough]
	public RentedBuffer(int minimumLength, bool useEntireCapacity)
		: this(minimumLength: minimumLength, preValidated: false, useEntireCapacity: useEntireCapacity)
	{
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="RentedBuffer{T}"/> with the specified minimum length.
	/// </summary>
	/// <param name="minimumLength">The minimum length of the buffer to rent.</param>
	/// <param name="preValidated">Indicates that <paramref name="minimumLength"/> has been pre-guarded against being null or zero.</param>
	/// <param name="useEntireCapacity">When <see langword="true"/>, the buffer's logical length spans the full rented array; otherwise it is limited to <paramref name="minimumLength"/>.</param>
	internal RentedBuffer(bool preValidated, int minimumLength, bool useEntireCapacity)
	{
		if (!preValidated)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(minimumLength);
			if (minimumLength == 0)
			{
				_state = new();
				_length = 0;
				_array = null;
				_span = [];
				return;
			}
		}

		T[] array = Rent.Array<T>(minimumLength);
		_state = new() { IsRented = true };
		int length = useEntireCapacity ? array.Length : minimumLength;
		_length = length;
		_array = array;
		_span = array.AsUnsafeSpan(0, length);
	}
	/// <summary>
	/// Initializes a new instance of the RentedBuffer class using the specified span as the underlying buffer.
	/// </summary>
	/// <remarks>This constructor is intended for scenarios where a buffer is provided directly rather than rented
	/// from a pool. The buffer is not managed by the RentedBuffer instance and will not be disposed or returned when the
	/// instance is released.</remarks>
	/// <param name="nonRentedBuffer">A span of elements to use as the buffer. The buffer is not rented and will not be returned to a pool.</param>
	public RentedBuffer(Span<T> nonRentedBuffer)
	{
		_state = new();
		_length = nonRentedBuffer.Length;
		_array = null;
		_span = nonRentedBuffer;
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="RentedBuffer{T}"/> with the specified values.
	/// </summary>
	/// <param name="values">The values to copy into the buffer.</param>
	/// <param name="useEntireCapacity">When <see langword="true"/>, the buffer's logical length spans the full rented array; otherwise it is limited to the number of copied values.</param>
	[DebuggerStepThrough]
	internal RentedBuffer(scoped ReadOnlySpan<T> values, bool useEntireCapacity)
	{
		T[] array = Rent.Array<T>(values.Length);
		values.CopyTo(array);
		int length = useEntireCapacity ? array.Length : values.Length;
		_array = array;
		_span = array.AsSpan(0, length);
		_state = new() { IsRented = true };
		_length = length;
	}

	/// <summary>
	/// Disposes of the rented buffer, returning it to the pool if it was rented.
	/// </summary>
	public void Dispose()
	{
		T[]? array = _array;
		State state = _state;
		this = default;

		if (isRented(state.IsRented, array))
		{
			Rent.Return(array, clearArray: state.ClearOnDispose);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool isRented(bool isRented, T[]? array)
		{
			arrayIsNullWhenItShouldBe(array, isRented);
			return isRented;
		}

		[Conditional("DEBUG")]
		static void arrayIsNullWhenItShouldBe(T[]? array, bool rented)
		{
			Debug.Assert(rented || array is null, "The rented array should BE null when the state indicates it is not rented.");
		}
	}
	/// <summary>
	/// Returns an enumerator that iterates through the rented buffer.
	/// </summary>
	/// <returns>An enumerator for the buffer.</returns>
	[DebuggerStepThrough]
	public readonly Enumerator GetEnumerator()
	{
		return new Enumerator(_span);
	}

	/// <summary>
	/// Expands the buffer to at least the specified minimum capacity.
	/// </summary>
	/// <remarks>
	/// The contents of the buffer are preserved when resizing being copied to the new rented buffer. The old buffer is returned to the pool
	/// if it was rented.
	/// </remarks>
	/// <param name="newMinimumCapacity">The minimum capacity to expand the buffer to.</param>
	/// <returns>
	/// The capacity of the buffer after resizing, which may be greater than or equal to the specified <paramref name="newMinimumCapacity"/>.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="newMinimumCapacity"/> is less than the current <see cref="Length"/>.</exception>
	[MethodImpl(MethodImplOptions.NoInlining)]
	public int Resize(int newMinimumCapacity)
	{
		return this.ResizeInline(newMinimumCapacity);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal int ResizeInline(int newMinimumCapacity)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newMinimumCapacity);
		ArgumentOutOfRangeException.ThrowIfLessThan(newMinimumCapacity, _length);

		int cap = this.Capacity;
		if ((uint)newMinimumCapacity <= (uint)cap && _state.IsRented) // If we are under the current capacity, we don't need to resize.
		{
			_length = cap;
			_span = _array;
			return cap;
		}

		T[] newArray = Rent.Array<T>(newMinimumCapacity);
		var newSpan = newArray.AsUnsafeSpan(0);
		Copy(_span, newSpan);

		if (_state.IsRented)
		{
			T[] oldArray = _array!;
			Rent.Return(oldArray, _state.ClearOnDispose);
		}

		_state.IsRented = true;
		_array = newArray;
		_span = newArray;
		_length = newArray.Length;
		return newArray.Length;
	}

	/// <summary>
	/// Copies the contents of the source span to the destination span.
	/// </summary>
	/// <param name="source">The source span to copy from.</param>
	/// <param name="destination">The destination span to copy to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Copy(ReadOnlySpan<T> source, Span<T> destination)
	{
		// If the type contains references, we cannot use Unsafe.CopyBlockUnaligned because it would not correctly handle the references.
		// 'Unsafe.CopyBlockUnaligned' has no GC write barrier so the card table for that region is never marked 'dirty'. A later gen0 collection won't scan the destination as a root,
		// see the reference object as dead, and reclaim it - leaving a dangling reference in the buffer.
		// Compounding the issue, the destination is 100% of the time a pooled array which always get fast-tracked promoted to gen2 quickly.
		//
		// This whole setup was written though, because 9 times out of 10, T is a primitive type (e.g. - char).

		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) // This is a constant call at runtime meaning the JIT will only produce one of the 2 branches below for a given type T.
		{
			source.CopyTo(destination);
			return;
		}

		uint size = (uint)Unsafe.SizeOf<T>();
		uint sourceCount = size * (uint)source.Length;
		ref byte sourceRef = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(source));
		ref byte destinationRef = ref Unsafe.As<T, byte>(ref destination.Ref());

		Unsafe.CopyBlockUnaligned(ref destinationRef, ref sourceRef, sourceCount);
	}

	/// <summary>
	/// Forms a slice out of the current buffer, beginning at the specified starting index extending to the end of the buffer.
	/// </summary>
	/// <param name="start">The zero-based starting index of the slice in the buffer.</param>
	/// <returns>
	/// A new span that is a slice of this buffer starting at the specified index and extending to the end of the buffer.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;Length).
	/// </exception>
	public readonly Span<T> Slice(int start)
	{
		return _span.Slice(start);
	}
	/// <summary>
	/// Forms a slice out of the current buffer, beginning at the specified starting index and of the specified length.
	/// </summary>
	/// <param name="start">The zero-based starting index of the slice in the buffer.</param>
	/// <param name="length">The desired length of the slice (exclusive).</param>
	/// <returns>
	/// A new span that is a slice of this buffer, starts at the specified index, and has the specified length.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;Length).
	/// </exception>
	public readonly Span<T> Slice(int start, int length)
	{
		return _span.Slice(start, length);
	}
	/// <summary>
	/// Forms a slice out of the backing array, beginning at the specified starting index extending to the end of the buffer.
	/// </summary>
	/// <param name="start"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"/>
	/// <exception cref="InvalidOperationException">The current <see cref="RentedBuffer{T}"/> instance is not rented.</exception>
	public readonly Span<T> SliceBacking(int start)
	{
		ThrowIfNotRented(_state.IsRented);
		return _array.AsSpan(start);
	}
	/// <summary>
	/// Forms a slice out of the backing array, beginning at the specified starting index and of the specified length.
	/// </summary>
	/// <param name="start">
	/// The zero-based starting index of the slice in the buffer.
	/// </param>
	/// <param name="length">
	/// The desired length of the slice (exclusive).
	/// </param>
	/// <returns>
	/// A new span that is a slice of this buffer's backing , starts at the specified index, and has the specified length.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException"/>
	/// <exception cref="InvalidOperationException">The current <see cref="RentedBuffer{T}"/> instance is not rented.</exception>
	public readonly Span<T> SliceBacking(int start, int length)
	{
		ThrowIfNotRented(_state.IsRented);
		return _array.AsSpan(start, length);
	}
	/// <summary>
	/// Returns a string representation of the rented buffer.
	/// </summary>
	/// <returns>A string that indicates whether the buffer is rented and its length.</returns>
	[DebuggerStepThrough]
	public override readonly string ToString()
	{
		return _length == 0
			? EMPTY_TOSTRING
			: $"{{IsRented = {_state.IsRented}, Length = {_length}, Capacity = {this.Capacity}}}";
	}

	/// <exception cref="InvalidOperationException"></exception>
	private static void ThrowIfNotRented([DoesNotReturnIf(false)] bool isRented)
	{
		if (!isRented)
		{
			throw new InvalidOperationException("Cannot access the backing array as this instance is not rented.");
		}
	}

	/// <summary>
	/// Returns a read-only view over the populated region of the buffer.
	/// </summary>
	/// <returns>A <see cref="ReadOnlySpan{T}"/> spanning the buffer's logical length.</returns>
	public readonly ReadOnlySpan<T> AsSpan()
	{
		return _span;
	}

	private const string EMPTY_TOSTRING = "{IsEmpty = true}";

	/// <summary>
	/// Enumerates the elements of a <see cref="RentedBuffer{T}"/>.
	/// </summary>
	[DebuggerStepThrough, StructLayout(LayoutKind.Auto)]
	public ref struct Enumerator
	{
		private readonly ReadOnlySpan<T> _span;
		private readonly int _length;
		private int _index;

		/// <summary>
		/// Gets the current element in the collection.
		/// </summary>
		public readonly ref readonly T Current => ref _span[_index];

		/// <summary>
		/// Initializes the enumerator with the specified array.
		/// </summary>
		/// <param name="array">The array to enumerate.</param>
		internal Enumerator(ReadOnlySpan<T> array)
		{
			_span = array;
			_index = -1;
			_length = array.Length;
		}
		/// <summary>
		/// Advances the enumerator to the next element of the collection.
		/// </summary>
		/// <returns>
		///		<see langword="true"/> if the enumerator was successfully advanced to the next element; 
		///		<see langword="false"/> if the enumerator has passed the end of the collection.
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