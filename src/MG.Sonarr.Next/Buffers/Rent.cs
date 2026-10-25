namespace MG.Sonarr.Next.Buffers;

/// <summary>
/// Provides utility methods for renting and returning arrays from the shared array pool.
/// </summary>
/// <remarks>The methods in this class offer a convenient way to manage array pooling using <see
/// cref="System.Buffers.ArrayPool{T}"/>. Renting and returning arrays can help reduce memory allocations and improve
/// performance in scenarios where temporary arrays are frequently needed. Arrays obtained from the pool are not
/// guaranteed to be zero-initialized. Callers should clear the array if required before use.</remarks>
[DebuggerStepThrough]
public static class Rent
{
	/// <summary>
	/// Rents an array of the specified minimum length from the shared array pool.
	/// </summary>
	/// <remarks>The returned array is rented from <see cref="System.Buffers.ArrayPool{T}.Shared"/> and may contain
	/// uninitialized data. Callers should clear the array if required before use. When no longer needed, the array should
	/// be returned to the pool using <see cref="Return{T}(T[], bool)"/> to avoid unnecessary
	/// allocations.</remarks>
	/// <typeparam name="T">The type of elements in the array.</typeparam>
	/// <param name="minimumLength">The minimum number of elements in the returned array. Must be non-negative.</param>
	/// <returns>An array of type <typeparamref name="T"/> with a length greater than or equal to <paramref name="minimumLength"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T[] Array<T>(int minimumLength)
	{
		Debug.Assert(minimumLength > 0, "The minimum length must be greater than zero.");
		return ArrayPool<T>.Shared.Rent(minimumLength);
	}
	/// <summary>
	/// Returns an array to the shared array pool for reuse.
	/// </summary>
	/// <remarks>If the array is null or has a length of zero, this method does nothing. Arrays containing reference
	/// types or references are always cleared before being returned to the pool to prevent memory leaks.</remarks>
	/// <typeparam name="T">The type of elements in the array.</typeparam>
	/// <param name="array">The array to return to the pool. Can be null.</param>
	/// <param name="clearArray">true to clear the contents of the array before returning it to the pool; otherwise, false. If the array contains
	/// reference types or references, the array will be cleared regardless of this parameter.</param>
	public static void Return<T>(T[]? array, bool clearArray = false)
	{
		if (array is { Length: > 0 })
		{
			ArrayPool<T>.Shared.Return(array, clearArray || RuntimeHelpers.IsReferenceOrContainsReferences<T>());
		}
	}
}