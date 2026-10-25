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
	/// This method handles the resizing of the buffer, including allocating a new array if necessary and copying the existing elements to the new array.
	/// The old array is returned to the pool if it was rented, and the new buffer will have at least the specified capacity.
	/// </remarks>
	/// <typeparam name="T">The type of elements in the buffer.</typeparam>
	/// <param name="buffer">The buffer instance.</param>
	/// <param name="newSize">The new size for the buffer.</param>
	public static void Resize<T>(ref PooledBuffer<T> buffer, int newSize)
	{
		if (newSize <= buffer.Capacity) return;

		if (buffer.IsDefaultOrEmpty)
		{
			buffer = new(newSize, useEntireCapacity: true);
			return;
		}

		bool dispose = buffer.ClearOnDispose;
		var old = buffer.GetRawArray()!;

		try
		{
			buffer = new(newSize, useEntireCapacity: true);
			Array.Copy(old, buffer.GetRawArray()!, old.Length);
		}
		finally
		{
			Rent.Return(old, clearArray: dispose);
		}
	}
}
