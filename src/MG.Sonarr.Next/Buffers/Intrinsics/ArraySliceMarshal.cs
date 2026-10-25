namespace MG.Sonarr.Next.Buffers.Intrinsics;

/// <summary>
/// Provides methods for creating <see cref="ArraySlice{T}"/> instances from arrays with specified offsets and lengths,
/// bypassing constructor validations.
/// </summary>
/// <remarks>
/// This is useful for scenarios where performance is critical and the caller can guarantee the validity of the parameters.
/// </remarks>
public static class ArraySliceMarshal
{
	/// <summary>
	/// Creates an ArraySlice from the specified array, offset, and length.
	/// </summary>
	/// <remarks>
	/// This method does not perform any bounds checking on the provided offset and length.
	/// It is the caller's responsibility to ensure that the offset and length are valid for the given array.
	/// If the offset and length are out of bounds, the behavior is undefined and may result in exceptions or data corruption.
	/// </remarks>
	/// <typeparam name="T">The type of elements in the array.</typeparam>
	/// <param name="array">The array to create the slice from. Cannot be null.</param>
	/// <param name="offset">The zero-based index in the array at which the slice begins. Must be non-negative and within the bounds of the array.</param>
	/// <param name="length">The number of elements in the slice. Must be non-negative and not exceed the number of elements from offset to the end of the array.</param>
	/// <returns>An <see cref="ArraySlice{T}"/> representing the specified segment of the array.</returns>
	public static ArraySlice<T> Create<T>(T[] array, int offset, int length)
	{
		Debug.Assert(array is not null, "Array cannot be null.");
		Debug.Assert(offset >= 0 && length >= 0 && offset + length <= array.Length, "Offset and length must be within the bounds of the array.");

		return new(array, new ArraySlice<T>.CtorArgs(pint.Clamp(offset), pint.Clamp(length)));
	}
}
