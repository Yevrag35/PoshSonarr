using System.ComponentModel;

namespace MG.Sonarr.Next.Buffers;

/// <summary>
/// Provides static factory methods for creating <see cref="ArraySlice{T}"/> instances.
/// </summary>
public static class ArraySlice
{
	/// <summary>
	/// Creates a new <see cref="ArraySlice{T}"/> containing the specified values.
	/// </summary>
	/// <remarks>The returned slice is backed by a new array containing the specified values. Use <see
	/// cref="Empty{T}()"/> to obtain a shared empty slice instance.</remarks>
	/// <typeparam name="T">The type of elements to include in the slice.</typeparam>
	/// <param name="values">A sequence of values to populate the slice. May be empty.</param>
	/// <returns>An <see cref="ArraySlice{T}"/> containing the provided values. Returns an empty slice if <paramref name="values"/>
	/// is empty.</returns>
	public static ArraySlice<T> Create<T>(params ReadOnlySpan<T> values)
	{
		if (values.IsEmpty)
			return Empty<T>();

		return new(values.ToArray(), 0, values.Length);
	}
	/// <summary>
	/// Returns an empty slice of the specified array element type.
	/// </summary>
	/// <remarks>The returned slice has a length of zero. This instance
	/// can be reused wherever an empty slice is required.</remarks>
	/// <typeparam name="T">The type of elements in the array slice.</typeparam>
	/// <returns>An <see cref="ArraySlice{T}"/> instance over a span of zero elements.</returns>
	public static ArraySlice<T> Empty<T>() => EmptyInstance<T>.Default;

	private static class EmptyInstance<T>
	{
		internal static readonly ArraySlice<T> Default = new([]);
	}
}

/// <summary>
/// Represents a contiguous slice of an array, defined by an offset and length, without copying the underlying data.
/// </summary>
/// <remarks><see cref="ArraySlice{T}"/> provides a lightweight view over a segment of an array, allowing efficient access and
/// manipulation of a subset of its elements. The slice does not own the array; changes to the underlying array are
/// reflected in the slice. This type is useful for scenarios where working with subarrays is required without incurring
/// the cost of allocation or copying. <see cref="ArraySlice{T}"/> is a value type and is intended for performance-critical code. It
/// is not thread-safe if the underlying array is modified concurrently.</remarks>
/// <typeparam name="T">The type of elements contained in the array slice.</typeparam>
[DebuggerDisplay("Length = {Length}")]
[CollectionBuilder(typeof(ArraySlice), nameof(ArraySlice.Create))]
public readonly struct ArraySlice<T> : IReadOnlyCollection<T>
{
	[StructLayout(LayoutKind.Auto)]
	internal readonly record struct CtorArgs(pint Offset, pint Length);

	private readonly T[]? _array;
	private readonly pint _length;
	private readonly pint _offset;

	/// <summary>
	/// Gets or sets the element at the specified index within the slice.
	/// </summary>
	/// <param name="index">The zero-based index of the element, relative to the start of the slice.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative or not less than
	/// <see cref="Length"/>.</exception>
	/// <value>The element located at the specified <paramref name="index"/>.</value>
	public T this[int index]
	{
		get
		{
			ThrowIfOutsideSlice(index, _length);
			return _array![_offset + index];
		}
		set
		{
			ThrowIfOutsideSlice(index, _length);
			_array![_offset + index] = value;
		}
	}

	/// <summary>
	/// Gets the underlying array represented by this slice.
	/// </summary>
	/// <value>
	/// The array that backs the slice, including its elements outside the slice, or <see langword="null"/> if the slice
	/// is default-initialized.
	/// </value>
	public T[]? Array => _array;
	/// <summary>
	/// Indicates whether the current <see cref="ArraySlice{T}"/> instance is default-initialized, meaning it has not been initialized with an underlying array.
	/// </summary>
	[MemberNotNullWhen(false, nameof(_array), nameof(Array))]
	public bool IsDefault => _array is null;
	/// <summary>
	/// Gets the number of elements in the slice.
	/// </summary>
	public readonly pint Length => _length;
	/// <summary>
	/// Gets the zero-based offset in the underlying array where the slice begins.
	/// </summary>
	public readonly pint Offset => _offset;

	/// <inheritdoc/>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	int IReadOnlyCollection<T>.Count => _length;

	/// <summary>
	/// Initializes a new instance of the ArraySlice class that represents an empty slice over the specified array.
	/// </summary>
	/// <remarks>This constructor is intended for internal use to create an ArraySlice that represents an empty
	/// collection. The provided array must have a length of zero.</remarks>
	/// <param name="empty">An array that must be empty. Used as the underlying storage for the empty slice.</param>
	internal ArraySlice(T[] empty)
	{
		Debug.Assert(empty.Length == 0);
		_array = empty;
		_length = 0;
		_offset = 0;
	}
	/// <summary>
	/// Initializes a new instance of the ArraySlice class that represents a slice of the specified array, using the provided offset and length.
	/// </summary>
	/// <param name="array">The array to create the slice from. Cannot be null.</param>
	/// <param name="args">A <see cref="CtorArgs"/> struct containing the offset and length for the slice.</param>
	internal ArraySlice(T[] array, CtorArgs args)
	{
		Debug.Assert(array is not null, "We're trusting you here...");
		_array = array;
		(_offset, _length) = args;
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="ArraySlice{T}"/> struct that represents a slice of the specified array,
	/// starting at the beginning and containing the specified number of elements.
	/// </summary>
	/// <param name="array">The array to create the slice from. Cannot be null.</param>
	/// <param name="length">The number of elements to include in the slice. Must be non-negative and not greater than the length of the array.</param>
	public ArraySlice(T[] array, int length) : this(array, 0, length)
	{
	}
	/// <summary>
	/// Initializes a new <see cref="ArraySlice{T}"/> instance over the <paramref name="length"/> elements of
	/// <paramref name="array"/> that start at <paramref name="offset"/>.
	/// </summary>
	/// <param name="array">The array to create a slice from. Cannot be null.</param>
	/// <param name="offset">The zero-based index in the array at which the slice begins. Must be greater than or equal to 0 and less than or
	/// equal to the length of the array.</param>
	/// <param name="length">The number of elements in the slice. Must be non-negative and not exceed the number of elements from offset to the
	/// end of the array.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="offset"/> is negative or greater than the length of
	/// <paramref name="array"/>, or when <paramref name="length"/> is negative or greater than the number of elements from
	/// <paramref name="offset"/> to the end of <paramref name="array"/>.</exception>
	public ArraySlice(T[] array, int offset, int length)
	{
		ArgumentNullException.ThrowIfNull(array);

		// Comparing as unsigned values rejects a negative argument along with one that's too large, and the subtraction
		// can't overflow once the offset is known to lie within the array.
		if ((uint)offset > (uint)array.Length || (uint)length > (uint)(array.Length - offset))
		{
			ThrowOutOfRange(offset, length, array.Length);
		}

		_array = array;
		_offset = new pint(offset, true);
		_length = new pint(length, true);
	}

	/// <summary>
	/// Deconstructs the slice into its underlying array, length, and offset components.
	/// </summary>
	/// <param name="array">When this method returns, contains the underlying array, or an empty array if the slice is
	/// default-initialized.</param>
	/// <param name="length">When this method returns, contains the number of elements in the slice.</param>
	/// <param name="offset">When this method returns, contains the zero-based offset at which the slice begins.</param>
	[DebuggerStepThrough, EditorBrowsable(EditorBrowsableState.Never)]
	public void Deconstruct(out T[] array, out int length, out int offset)
	{
		array = this.Array ?? [];
		length = _length;
		offset = _offset;
	}

	/// <summary>
	/// Returns a read-only span over the valid segment of the underlying array.
	/// </summary>
	/// <remarks>The returned span reflects the current state of the underlying array segment. Modifications to the
	/// array after obtaining the span are visible through the span. The span does not allocate memory.</remarks>
	/// <returns>A <see cref="ReadOnlySpan{T}"/> representing the elements in the current segment. Returns an empty span if the
	/// segment is empty.</returns>
	public ReadOnlySpan<T> AsSpan()
	{
		return _length > 0
			? _array!.AsUnsafeSpan(_offset, _length)
			: [];
	}

	/// <summary>
	/// Converts an <see cref="ArraySlice{T}"/> instance to a <see cref="ReadOnlySpan{T}"/> representing the sliced portion
	/// of the array.
	/// </summary>
	/// <remarks>If the underlying array is null or empty, the resulting <see cref="ReadOnlySpan{T}"/> will be
	/// empty. This operator enables seamless use of <see cref="ArraySlice{T}"/> in APIs that accept <see
	/// cref="ReadOnlySpan{T}"/>.</remarks>
	/// <param name="slice">The <see cref="ArraySlice{T}"/> to convert to a <see cref="ReadOnlySpan{T}"/>.</param>
	public static implicit operator ReadOnlySpan<T>(ArraySlice<T> slice)
	{
		return slice._array is T[] array && array.Length > 0
			? new ReadOnlySpan<T>(array, slice._offset, slice._length)
			: [];
	}
	/// <summary>
	/// Converts an <see cref="ArraySlice{T}"/> instance to a <see cref="Span{T}"/> representing the sliced portion of the
	/// array.
	/// </summary>
	/// <remarks>If the underlying array is null or empty, the resulting <see cref="Span{T}"/> will be empty. This
	/// operator enables seamless use of <see cref="ArraySlice{T}"/> in APIs that accept <see cref="Span{T}"/>.</remarks>
	/// <param name="slice">The <see cref="ArraySlice{T}"/> to convert to a <see cref="Span{T}"/>.</param>
	public static implicit operator Span<T>(ArraySlice<T> slice)
	{
		return slice._array is T[] array && array.Length > 0
			? array.AsSpan(slice._offset, slice._length)
			: [];
	}

	/// <summary>
	/// Returns an enumerator that iterates through the <see cref="ArraySlice{T}"/>
	/// </summary>
	/// <returns>An enumerator that can be used to iterate through the contiguous slice.</returns>
	[DebuggerStepThrough]
	public Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}
	/// <inheritdoc/>
	[DebuggerStepThrough]
	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return this.GetEnumerator();
	}
	/// <inheritdoc/>
	[DebuggerStepThrough]
	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}

	/// <summary>
	/// Throws an <see cref="ArgumentOutOfRangeException"/> when <paramref name="index"/> doesn't lie within a slice of
	/// <paramref name="length"/> elements.
	/// </summary>
	/// <param name="index">The zero-based index, relative to the start of the slice.</param>
	/// <param name="length">The number of elements in the slice.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative or not less than
	/// <paramref name="length"/>.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ThrowIfOutsideSlice(int index, int length)
	{
		if ((uint)index >= (uint)length)
		{
			ThrowIndexOutOfRange(index);
		}
	}

	/// <summary>
	/// Throws an <see cref="ArgumentOutOfRangeException"/> for an index that lies outside the slice.
	/// </summary>
	/// <param name="index">The index that lies outside the slice.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown unconditionally.</exception>
	[DoesNotReturn, StackTraceHidden, DebuggerStepThrough, MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowIndexOutOfRange(int index)
	{
		throw new ArgumentOutOfRangeException(nameof(index), index, "The index must lie within the slice.");
	}

	/// <summary>
	/// Throws an <see cref="ArgumentOutOfRangeException"/> that names the argument that puts a slice outside its array.
	/// </summary>
	/// <param name="offset">The zero-based index at which the slice begins.</param>
	/// <param name="length">The number of elements in the slice.</param>
	/// <param name="arrayLength">The length of the array.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown unconditionally.</exception>
	[DoesNotReturn, StackTraceHidden, DebuggerStepThrough, MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowOutOfRange(int offset, int length, int arrayLength)
	{
		throw (uint)offset > (uint)arrayLength
			? new ArgumentOutOfRangeException(nameof(offset), offset, "The offset must lie within the array.")
			: new ArgumentOutOfRangeException(nameof(length), length, "The slice must end within the array.");
	}

	/// <summary>
	/// Provides an enumerator for iterating over a slice of an array of type <typeparamref name="T"/>.
	/// </summary>
	[StructLayout(LayoutKind.Auto)]
	public struct Enumerator : IEnumerator<T>
	{
		private T[] _array;
		private T _current;
		private int _index;
		private int _length;
		private int _offset;

		/// <summary>
		/// Initializes a new instance of the Enumerator for the specified array slice.
		/// </summary>
		/// <param name="slice">The array slice to enumerate. Must contain a valid array and range.</param>
		internal Enumerator(ArraySlice<T> slice)
		{
			_array = slice.Array!;
			_current = default!;
			_index = -1;
			_length = slice._length;
			_offset = slice._offset;
		}

		/// <summary>
		/// Gets the current value of type <typeparamref name="T"/>.
		/// </summary>
		public readonly T Current => _current;

		/// <inheritdoc/>
		public bool MoveNext()
		{
			int next = _index + 1;
			if ((uint)next < (uint)_length)
			{
				_current = _array[_offset + next];
				_index = next;
				return true;
			}

			_index = _length;
			_current = default!;
			return false;
		}
		/// <inheritdoc/>
		public void Reset()
		{
			_current = default!;
			_index = -1;
		}
		/// <inheritdoc/>
		public readonly void Dispose()
		{
		}

		/// <inheritdoc/>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly object? IEnumerator.Current => this.Current;
	}
}
