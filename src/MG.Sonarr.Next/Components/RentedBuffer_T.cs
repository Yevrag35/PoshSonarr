using MG.Sonarr.Next.Guarding;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Components;

/// <summary>
/// Represents a rented buffer of type <typeparamref name="T"/>, providing access to its underlying span and handling disposal to return it to the pool.
/// </summary>
/// <typeparam name="T">The type of elements in the buffer.</typeparam>
[StructLayout(LayoutKind.Auto)]
[CollectionBuilder(typeof(RentedBuffer), nameof(RentedBuffer.Create))]
public ref partial struct RentedBuffer<T>
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private int _length;
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private int _capacity;
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private bool _isRented;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private Span<T> _span;
	private T[]? _array;

	/// <summary>
	/// Gets the reference of the element at the specified index in the buffer.
	/// </summary>
	/// <remarks>
	/// The index must be within the bounds of the minimum length and not the capacity of the buffer.
	/// </remarks>
	/// <param name="index">The zero-based index of the element to get.</param>
	/// <returns>A reference to the element of type <typeparamref name="T"/> at the specified index in the buffer.</returns>
	/// <exception cref="IndexOutOfRangeException"/>
	public readonly ref T this[int index] => ref _span[index];

	/// <summary>
	/// Gets the span of elements in the rented buffer.
	/// </summary>
	public readonly Span<T> Buffer => _span;
	/// <summary>
	/// The number of elements that the buffer can store.
	/// </summary>
	public readonly int Capacity => _capacity;
	/// <summary>
	/// Gets the length of the specified minimum length.
	/// </summary>
	/// <remarks>
	/// This value will always be less than or equal to the capacity of the buffer.
	/// </remarks>
	public readonly int Length => _length;
	/// <summary>
	/// Gets a value indicating whether the buffer is rented from the pool.
	/// </summary>
	[MemberNotNullWhen(true, nameof(_array))]
	public readonly bool IsRented => _isRented;

	#region CONSTRUCTORS

	/// <summary>
	/// Initializes a new instance of the <see cref="RentedBuffer{T}"/> with the specified minimum length.
	/// </summary>
	/// <param name="minimumLength">The minimum length of the buffer to rent.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minimumLength"/> is negative.</exception>
	[DebuggerStepThrough]
	public RentedBuffer(int minimumLength)
		: this(minimumLength, preValidated: false)
	{
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="RentedBuffer{T}"/> with the specified minimum length.
	/// </summary>
	/// <param name="minimumLength">The minimum length of the buffer to rent.</param>
	/// <param name="preValidated">Indicates that <paramref name="minimumLength"/> has been pre-guarded against being null or zero.</param>
	internal RentedBuffer(int minimumLength, bool preValidated)
	{
		if (!preValidated)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(minimumLength);
			if (minimumLength == 0)
			{
				_isRented = false;
				_length = 0;
				_capacity = 0;
				_array = null;
				_span = [];
			}
		}

		T[] array = ArrayPool<T>.Shared.Rent(minimumLength);
		_isRented = true;
		_length = minimumLength;
		_array = array;
		_capacity = array.Length;
		_span = array.AsSpan(0, minimumLength);
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="RentedBuffer{T}"/> with the pre-allocated buffer.
	/// </summary>
	/// <remarks>
	/// When this constructor is used, the buffer is not considered "rented" - meaning it will not be returned to the pool when disposed.
	/// </remarks>
	/// <param name="nonRentedBuffer">The pre-allocated buffer to use.</param>
	internal RentedBuffer(Span<T> nonRentedBuffer)
	{
		_isRented = false;
		_length = nonRentedBuffer.Length;
		_capacity = nonRentedBuffer.Length;
		_array = null;
		_span = nonRentedBuffer;
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="RentedBuffer{T}"/> with the specified values copied to an internally rented buffer.
	/// </summary>
	/// <param name="values">The values to copy into the internal buffer.</param>
	[DebuggerStepThrough]
	internal RentedBuffer(scoped ReadOnlySpan<T> values)
	{
		T[] array = ArrayPool<T>.Shared.Rent(values.Length);
		values.CopyTo(array);
		_array = array;
		_span = array.AsSpan(0, values.Length);
		_isRented = true;
		_length = values.Length;
		_capacity = array.Length;
	}

	#endregion

	/// <summary>
	/// Copies the elements of this buffer to the specified destination.
	/// </summary>
	/// <param name="destination"></param>
	/// <exception cref="ArgumentException"/>
	public readonly void CopyTo(Span<T> destination)
	{
		Guard.BufferLengthIsAtLeast<T>(destination, _span.Length);
		_span.CopyTo(destination);
	}
	public readonly void CopyTo(ref RentedBuffer<T> destination)
	{
		Guard.BufferLengthIsAtLeast<T>(destination._span, _span.Length, nameof(destination));

		this.UnsafeCopyTo(ref destination);
	}
	internal readonly void UnsafeCopyTo(ref RentedBuffer<T> destination)
	{
		_span.CopyTo(destination._span);
	}
	internal readonly void UnsafeCopyTo(ref RentedBuffer<T> destination, int index, int length)
	{
		_span.Slice(index, length).CopyTo(destination._span);
	}

	/// <summary>
	/// Provides unsafe access to the backing array of the buffer.
	/// </summary>
	/// <returns>
	/// The backing array of type <typeparamref name="T"/> if the buffer is rented; otherwise, <see langword="null"/>.
	/// </returns>
	[ExcludeFromCodeCoverage]
	internal readonly T[]? DangerousGetBackingArray()
	{
		return _array;
	}
	/// <summary>
	/// Disposes of the rented buffer, returning it to the pool if it was rented.
	/// </summary>
	public void Dispose()
	{
		T[]? array = _array;
		bool rented = _isRented;
		this = default;

		if (rented)
		{
			ArrayPool<T>.Shared.Return(array!);
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
	/// <param name="length">The desiered length of the slice (exclusive).</param>
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
		ThrowIfNotRented(_isRented);
		return _array.AsSpan(start);
	}
	/// <summary>
	/// Forms a slice out of the backing array, beginning at the specified starting index and of the specified length.
	/// </summary>
	/// <param name="start">
	/// The zero-based starting index of the slice in the buffer.
	/// </param>
	/// <param name="length">
	/// The desiered length of the slice (exclusive).
	/// </param>
	/// <returns>
	/// A new span that is a slice of this buffer's backing , starts at the specified index, and has the specified length.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException"/>
	/// <exception cref="InvalidOperationException">The current <see cref="RentedBuffer{T}"/> instance is not rented.</exception>
	public readonly Span<T> SliceBacking(int start, int length)
	{
		ThrowIfNotRented(_isRented);
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
			: $"{{IsRented = {_isRented}, Length = {_length}, Capacity = {_capacity}}}";
	}

	/// <exception cref="InvalidOperationException"></exception>
	private static void ThrowIfNotRented([DoesNotReturnIf(false)] bool isRented)
	{
		if (!isRented)
		{
			throw new InvalidOperationException("Cannot access the backing array as this instance is not rented.");
		}
	}

	private const string EMPTY_TOSTRING = "{IsEmpty = true}";
}