using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Buffers;

/// <summary>
/// Provides methods for creating and managing rented buffers using the shared <see cref="ArrayPool{T}"/>.
/// </summary>
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
	/// Rents a buffer with at least the specified minimum length, updating the provided buffer reference.
	/// </summary>
	/// <typeparam name="T">The type of elements in the buffer.</typeparam>
	/// <param name="minimumLength">The minimum length of the rented buffer.</param>
	/// <param name="buffer">The buffer reference to update.</param>
	/// <returns>A span containing the rented buffer up to the specified length.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minimumLength"/> is negative.</exception>
	public static Span<T> Rent<T>(int minimumLength, ref RentedBuffer<T> buffer)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(minimumLength);
		if (minimumLength == 0)
		{
			buffer = Empty<T>();
			return [];
		}

		buffer = new(minimumLength: minimumLength, preValidated: true, useEntireCapacity: false);
		return buffer.Span;
	}
	/// <summary>
	/// Rents a buffer with either the specified span or an <see cref="int"/> length that will be rented from <see cref="ArrayPool{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of elements in the buffer.</typeparam>
	/// <param name="either">A discriminated union of either a span or an integer length.</param>
	/// <returns>A <see cref="RentedBuffer{T}"/> with either a backing <see cref="Span{T}"/> or a rented array buffer, 
	/// depending on what was provided in <paramref name="either"/>.</returns>
	public static RentedBuffer<T> Rent<T>(RefEither<Span<T>, int> either)
	{
		return either.Index switch
		{
			1 => new(either.AsT1),
			2 when either.AsT2 > 0 => new(preValidated: true, minimumLength: either.AsT2, useEntireCapacity: true),
			_ => Empty<T>(),
		};
	}
}