namespace MG.Sonarr.Next.Components;

/// <summary>
/// Provides methods for creating and managing rented buffers using the shared <see cref="System.Buffers.ArrayPool{T}"/>.
/// </summary>
public static class RentedBuffer
{
	/// <summary>
	/// Creates a <see cref="RentedBuffer{T}"/> initialized with the specified values.
	/// </summary>
	/// <typeparam name="T">The type of elements in the buffer.</typeparam>
	/// <param name="values">A read-only span of values to initialize the buffer with.</param>
	/// <returns>A <see cref="RentedBuffer{T}"/> containing the specified values.</returns>
	public static RentedBuffer<T> Create<T>(ReadOnlySpan<T> values)
	{
		return !values.IsEmpty
			? new(values)
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
	public static RentedBuffer<T> Rent<T>(int minimumLength)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(minimumLength);
		if (minimumLength == 0)
		{
			return Empty<T>();
		}

		return new(minimumLength, preValidated: true);
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

		buffer = new(minimumLength, preValidated: true);
		return buffer.Slice(0, minimumLength);
	}

	///// <summary>
	///// 
	///// </summary>
	///// <typeparam name="T"></typeparam>
	///// <param name="eitherSpanOrLength"></param>
	///// <returns></returns>
	//public static RentedBuffer<T> Rent<T>(RefEither<Span<T>, int> eitherSpanOrLength)
	//{
	//	return eitherSpanOrLength.Index switch
	//	{
	//		1u => new(eitherSpanOrLength.AsT1),
	//		2u => Rent<T>(eitherSpanOrLength.AsT2),
	//		0u or _ => Empty<T>(),
	//	};
	//}
}