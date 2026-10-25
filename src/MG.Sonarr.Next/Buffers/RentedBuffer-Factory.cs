using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Buffers;

/// <summary>
/// Provides methods for creating and managing rented buffers using the shared <see cref="ArrayPool{T}"/>.
/// </summary>
[DebuggerStepThrough]
public static class RentedBuffer
{
	/// <summary>
	/// Creates a <see cref="RentedBuffer{T}"/> initialized with the specified values.
	/// </summary>
	/// <typeparam name="T">The type of elements in the buffer.</typeparam>
	/// <param name="values">A read-only span of values to initialize the buffer with.</param>
	/// <returns>A <see cref="RentedBuffer{T}"/> containing the specified values.</returns>
	public static RentedBuffer<T> Create<T>(params ReadOnlySpan<T> values)
	{
		return !values.IsEmpty
			? new(values, useEntireCapacity: false)
			: Empty<T>();
	}
	/// <summary>
	/// Returns an empty instance of <see cref="RentedBuffer{T}"/>.
	/// </summary>
	public static RentedBuffer<T> Empty<T>() => default;

	/// <summary>
	/// Rents a buffer with at least the specified minimum length.
	/// </summary>
	/// <typeparam name="T"><inheritdoc cref="RentedBuffer{T}" path="/typeparam[1]"/></typeparam>
	/// <param name="minimumLength">The minimum number of elements the buffer can hold.</param>
	/// <returns>A <see cref="RentedBuffer{T}"/> with at least the specified minimum capacity.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minimumLength"/> is negative.</exception>
	[DebuggerStepThrough]
	public static RentedBuffer<T> RentMin<T>(int minimumLength)
	{
		return Rent<T>(minimumLength, useEntireCapacity: false);
	}
	/// <summary>
	/// Creates a <see cref="RentedBuffer{T}"/> that wraps an existing span without renting from the pool.
	/// </summary>
	/// <typeparam name="T"><inheritdoc cref="RentedBuffer{T}" path="/typeparam[1]"/></typeparam>
	/// <param name="span">The span to wrap. The returned buffer references this storage and is never returned to a pool.</param>
	/// <returns>A <see cref="RentedBuffer{T}"/> backed by <paramref name="span"/>.</returns>
	public static RentedBuffer<T> RentSpan<T>(Span<T> span)
	{
		return new(span);
	}
	/// <summary>
	/// Rents a buffer with at least the specified minimum length and a flag indicating whether to use the entire capacity of the buffer.
	/// </summary>
	/// <typeparam name="T"><inheritdoc cref="RentedBuffer{T}" path="/typeparam[1]"/></typeparam>
	/// <param name="minimumLength">The minimum number of elements the buffer can hold.</param>
	/// <param name="useEntireCapacity">Indicates whether to make the entire capacity of the buffer available.</param>
	/// <returns>A <see cref="RentedBuffer{T}"/> with at least the specified minimum capacity.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minimumLength"/> is negative.</exception>
	public static RentedBuffer<T> Rent<T>(int minimumLength, bool useEntireCapacity)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(minimumLength);
		if (minimumLength == 0)
		{
			return Empty<T>();
		}

		return new(minimumLength: minimumLength, preValidated: true, useEntireCapacity: useEntireCapacity);
	}
	/// <summary>
	/// Rents a buffer with either the specified span or an <see cref="int"/> length that will be rented from <see cref="ArrayPool{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of elements in the buffer.</typeparam>
	/// <param name="either">A discriminated union of either a span or an integer length.</param>
	/// <returns>A <see cref="RentedBuffer{T}"/> with either a backing <see cref="Span{T}"/> or a rented array buffer, 
	/// depending on what was provided in <paramref name="either"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static RentedBuffer<T> Rent<T>(RefEither<Span<T>, int> either)
	{
		return either.IsT1 ? new(either.AsT1) : rentFromLength(either.AsT2);

		[MethodImpl(MethodImplOptions.NoInlining)]
		static RentedBuffer<T> rentFromLength(int length)
		{
			return Rent<T>(length, useEntireCapacity: false);
		}
	}
}
