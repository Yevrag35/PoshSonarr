namespace MG.Sonarr.Next.Buffers.Intrinsics;

/// <summary>
/// Provides low-level operations for working with <see cref="PooledBuffer{T}"/> instances, including reference retrieval and resizing.
/// </summary>
[DebuggerStepThrough]
public static class PoolMarshal
{
	/// <summary>
	/// Creates a new <see cref="PooledBuffer{T}"/> instance with the specified minimum size and an option to use the entire capacity of the underlying array.
	/// </summary>
	/// <typeparam name="T">The type of elements in the buffer.</typeparam>
	/// <param name="minimumSize">The minimum size of the buffer to create.</param>
	/// <param name="useEntireCapacity">If <see langword="true"/>, the buffer's length will match the full capacity of the underlying array; otherwise, it will match <paramref name="minimumSize"/>.</param>
	/// <returns>A new <see cref="PooledBuffer{T}"/> instance.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PooledBuffer<T> UnsafeCreate<T>(int minimumSize, bool useEntireCapacity = false)
	{
		return new(@internal: true, minimumSize, useEntireCapacity);
	}

	/// <summary>
	/// Returns a reference to the first element of the underlying array of a <see cref="PooledBuffer{T}"/> instance.
	/// </summary>
	/// <typeparam name="T">The type of elements in the buffer.</typeparam>
	/// <param name="buffer">The buffer instance.</param>
	/// <returns>A reference to the first element of the underlying array, or a <see langword="null"/> 
	/// reference if the buffer is <see langword="default"/> or empty.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref T GetReference<T>(ref readonly PooledBuffer<T> buffer)
	{
		return ref !buffer.IsDefaultOrEmpty
			? ref MemoryMarshal.GetArrayDataReference(buffer.GetRawArray()!)
			: ref Unsafe.NullRef<T>();
	}

	/// <summary>
	/// Resizes the specified <see cref="PooledBuffer{T}"/> instance to the specified new size.
	/// </summary>
	/// <remarks>
	/// <para>The method never shrinks a buffer, so one whose <see cref="PooledBuffer{T}.Length"/> already covers
	/// <paramref name="newSize"/> doesn't change. Any other buffer ends with a <see cref="PooledBuffer{T}.Length"/> that
	/// covers its whole backing array: the array that it has, when that array can hold <paramref name="newSize"/>
	/// elements, and otherwise a newly rented array of at least that size, into which the old array is copied before the
	/// old array goes back to the pool.</para>
	/// <para>The buffer keeps its <see cref="PooledBuffer{T}.ClearOnDispose"/> setting, which also decides whether the
	/// old array is cleared on its way back to the pool.</para>
	/// </remarks>
	/// <typeparam name="T">The type of elements in the buffer.</typeparam>
	/// <param name="buffer">The buffer instance.</param>
	/// <param name="newSize">The number of elements that the buffer must be able to hold.</param>
	public static void Resize<T>(ref PooledBuffer<T> buffer, int newSize)
	{
		buffer.ResizeCore(newSize);
	}
}
