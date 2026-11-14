namespace MG.Sonarr.Next.Buffers.Intrinsics;

/// <summary>
/// Provides methods for creating <see cref="ArraySlice{T}"/> instances from arrays with specified offsets and lengths,
/// bypassing constructor validations.
/// </summary>
/// <remarks>
/// This is useful for scenarios where performance is critical and the caller can guarantee the validity of the parameters.
/// </remarks>
[SharedCode]
[ExcludeFromCodeCoverage]
internal static class ArraySliceMarshal
{
	/// <summary>
	/// Creates an ArraySlice from the specified array, offset, and length.
	/// </summary>
	/// <remarks>
	/// This method does not perform any bounds checking on the provided offset and length.
	/// It is the caller's responsibility to ensure that the offset and length are valid for the given array.
	/// If the offset and length are out of bounds, the behavior is undefined and may result in exceptions or data corruption.
	/// </remarks>
	/// <safety>
	/// <paramref name="array"/> must not be <see langword="null"/>, <paramref name="offset"/> and <paramref name="length"/>
	/// must not be negative, and their sum must not exceed the length of <paramref name="array"/>. The slice reads its
	/// elements without bounds checks, trusting these values for as long as it exists.
	/// </safety>
	/// <typeparam name="T">The type of elements in the array.</typeparam>
	/// <param name="array">The array to create the slice from. Cannot be null.</param>
	/// <param name="offset">The zero-based index in the array at which the slice begins. Must be non-negative and within the bounds of the array.</param>
	/// <param name="length">The number of elements in the slice. Must be non-negative and not exceed the number of elements from offset to the end of the array.</param>
	/// <returns>An <see cref="ArraySlice{T}"/> representing the specified segment of the array.</returns>
	[SuppressMessage("Style", "IDE0380", Justification = "Skips the constructor's range checks, and the slice reads its elements without bounds checks, so an offset or length outside the array reads memory outside it.")]
	public static unsafe ArraySlice<T> Create<T>(T[] array, int offset, int length)
	{
		Debug.Assert(array is not null, "Array cannot be null.");
		Debug.Assert(offset >= 0 && length >= 0 && offset + length <= array.Length, "Offset and length must be within the bounds of the array.");

		unsafe
		{
			// SAFETY: The caller meets the constructor's contract, which is this method's own.
			return new(array, new ArraySlice<T>.CtorArgs(pint.Clamp(offset), pint.Clamp(length)));
		}
	}
}
