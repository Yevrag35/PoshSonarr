namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides bounds-check-free span slicing helpers that bypass the CLR's normal range validation.
/// </summary>
/// <remarks>
/// <para>
/// No runtime bounds checks are performed; callers must guarantee that all index and length arguments
/// are within range. In debug builds, <see cref="Debug"/> assertions verify preconditions; in release
/// builds, out-of-range arguments produce undefined behavior.
/// </para>
/// <para><b>Performance:</b> Every member is decorated with <see cref="MethodImplOptions.AggressiveInlining"/>
/// to keep hot-path call stacks clean.</para>
/// </remarks>
[DebuggerStepThrough]
internal static class SpanUnsafeExtensions
{
	/// <summary>
	/// Provides extension methods for <see cref="ReadOnlySpan{T}"/> that allow for bounds-check-free element access and copying.
	/// </summary>
	/// <typeparam name="T">The type of elements in the span.</typeparam>
	/// <param name="source">The span to operate on.</param>
	extension<T>(ReadOnlySpan<T> source)
	{
		/// <summary>
		/// Returns a reference to the element at <paramref name="index"/> in <paramref name="source"/>,
		/// without performing runtime bounds checks.
		/// </summary>
		/// <param name="index">The zero-based index of the element to reference.</param>
		/// <returns>A reference to the element at the specified index.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref readonly T Ref(int index = 0)
		{
			Debug.Assert((uint)index < (uint)source.Length, "index out of range");
			return ref Unsafe.Add(ref MemoryMarshal.GetReference(source), index);
		}

		/// <summary>
		/// Copies the contents of this <see cref="ReadOnlySpan{T}"/> to the specified destination <see cref="Span{T}"/>,
		/// without performing runtime bounds checks, even if the source and destination overlap.
		/// </summary>
		/// <remarks>
		/// <para>Slicing the destination to the source's exact length is what elides the check: it leaves
		/// <see cref="ReadOnlySpan{T}.CopyTo(Span{T})"/> comparing a length against itself, which the JIT folds
		/// away, so neither the comparison nor its throw path is emitted.</para>
		/// <para>The copy carries GC write barriers and tolerates overlapping regions, so it is correct for every
		/// <typeparamref name="T"/>. Do not rewrite it over <c>Unsafe.CopyBlockUnaligned</c>: that drops the write
		/// barriers a reference-containing <typeparamref name="T"/> needs, and buys nothing in exchange, because
		/// since net9 the <c>cpblk</c> it emits resolves through <c>CORINFO_HELP_MEMCPY</c> to the same
		/// <c>SpanHelpers.Memmove</c> this calls directly.</para>
		/// </remarks>
		/// <param name="destination">The destination span to copy to.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void UnsafeCopy(Span<T> destination)
		{
			Debug.Assert(source.Length <= destination.Length, "The destination is too short to hold the source.");
			source.CopyTo(destination.UnsafeSlice(0, source.Length));
		}
		/// <summary>
		/// Returns a <see cref="ReadOnlySpan{T}"/> that begins at <paramref name="start"/> and extends to the
		/// end of <paramref name="source"/>, without performing runtime bounds checks.
		/// </summary>
		/// <param name="start">The zero-based index at which the slice begins. Must satisfy <c>0 &lt;= start &lt;= source.Length</c>.</param>
		/// <returns>A <see cref="ReadOnlySpan{T}"/> equivalent to <c>source[start..]</c>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ReadOnlySpan<T> UnsafeSlice(int start)
		{
			return SliceCore(source, start, source.Length - start);
		}
		/// <summary>
		/// Returns a <see cref="ReadOnlySpan{T}"/> that begins at <paramref name="start"/> and covers
		/// <paramref name="length"/> elements, without performing runtime bounds checks.
		/// </summary>
		/// <param name="start">The zero-based index at which the slice begins.</param>
		/// <param name="length">The number of elements in the slice. Must satisfy <c>start + length &lt;= source.Length</c>.</param>
		/// <returns>A <see cref="ReadOnlySpan{T}"/> equivalent to <c>source[start..(start + length)]</c>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ReadOnlySpan<T> UnsafeSlice(int start, int length)
		{
			return SliceCore(source, start, length);
		}

		/// <summary>
		/// Creates a <see cref="ReadOnlySpan{T}"/> from a raw reference advanced by <paramref name="start"/>
		/// positions, bypassing all CLR bounds checks.
		/// </summary>
		/// <remarks>
		/// Uses <see cref="Unsafe.Add{T}(ref T, int)"/> to offset the span's pinned reference and
		/// <see cref="MemoryMarshal.CreateSpan{T}(ref T, int)"/> to construct the result without
		/// any range validation. In debug builds, <see cref="Debug.Assert(bool, string)"/> verifies that
		/// <paramref name="start"/> doesn't exceed <c>span.Length</c> and that <paramref name="length"/> is
		/// non-negative and ends within it. A <paramref name="start"/> equal to <c>span.Length</c> is in range
		/// and yields an empty span, matching <c>span[start..]</c>.
		/// </remarks>
		/// <param name="span">The source span.</param>
		/// <param name="start">The zero-based offset at which the slice begins.</param>
		/// <param name="length">The number of elements in the resulting slice.</param>
		/// <returns>A <see cref="ReadOnlySpan{T}"/> starting at <paramref name="start"/> with <paramref name="length"/> elements.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ReadOnlySpan<T> SliceCore(ReadOnlySpan<T> span, int start, int length)
		{
			Debug.Assert((uint)start <= (uint)span.Length, "start index out of range");
			Debug.Assert((ulong)(uint)start + (uint)length <= (uint)span.Length, "length out of range");
			return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), start), length);
		}
	}

	/// <summary>
	/// Provides extension methods for <see cref="Span{T}"/> that allow for bounds-check-free element access and slicing.
	/// </summary>
	/// <typeparam name="T">The type of elements in the span.</typeparam>
	/// <param name="span">The span to operate on.</param>
	extension<T>(Span<T> span)
	{
		/// <summary>
		/// Returns a reference to the element at <paramref name="index"/> in <paramref name="span"/>,
		/// without performing runtime bounds checks.
		/// </summary>
		/// <param name="index">The zero-based index of the element to reference.</param>
		/// <returns>A reference to the element at the specified index.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref T Ref(int index = 0)
		{
			Debug.Assert((uint)index < (uint)span.Length, "index out of range");
			return ref Unsafe.Add(ref MemoryMarshal.GetReference(span), index);
		}
		/// <summary>
		/// Returns a <see cref="Span{T}"/> that begins at <paramref name="start"/> and extends to the
		/// end of <paramref name="span"/>, without performing runtime bounds checks.
		/// </summary>
		/// <param name="start">The zero-based index at which the slice begins. Must satisfy <c>0 &lt;= start &lt;= span.Length</c>.</param>
		/// <returns>A <see cref="Span{T}"/> equivalent to <c>span[start..]</c>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<T> UnsafeSlice(int start)
		{
			return SliceMutableCore(span, start, span.Length - start);
		}

		/// <summary>
		/// Returns a <see cref="Span{T}"/> that begins at <paramref name="start"/> and covers
		/// <paramref name="length"/> elements, without performing runtime bounds checks.
		/// </summary>
		/// <param name="start">The zero-based index at which the slice begins.</param>
		/// <param name="length">The number of elements in the slice. Must satisfy <c>start + length &lt;= span.Length</c>.</param>
		/// <returns>A <see cref="Span{T}"/> equivalent to <c>span[start..(start + length)]</c>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<T> UnsafeSlice(int start, int length)
		{
			return SliceMutableCore(span, start, length);
		}

		/// <summary>
		/// Creates a <see cref="Span{T}"/> from a raw reference advanced by <paramref name="start"/>
		/// positions, bypassing all CLR bounds checks.
		/// </summary>
		/// <remarks>
		/// Uses <see cref="Unsafe.Add{T}(ref T, int)"/> to offset the span's pinned reference and
		/// <see cref="MemoryMarshal.CreateSpan{T}(ref T, int)"/> to construct the result without
		/// any range validation. In debug builds, <see cref="Debug.Assert(bool, string)"/> verifies that
		/// <paramref name="start"/> doesn't exceed <c>source.Length</c> and that <paramref name="length"/> is
		/// non-negative and ends within it. A <paramref name="start"/> equal to <c>source.Length</c> is in range
		/// and yields an empty span, matching <c>source[start..]</c>.
		/// </remarks>
		/// <param name="source">The source span.</param>
		/// <param name="start">The zero-based offset at which the slice begins.</param>
		/// <param name="length">The number of elements in the resulting slice.</param>
		/// <returns>A <see cref="Span{T}"/> starting at <paramref name="start"/> with <paramref name="length"/> elements.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Span<T> SliceMutableCore(Span<T> source, int start, int length)
		{
			Debug.Assert((uint)start <= (uint)source.Length, "start index out of range");
			Debug.Assert((ulong)(uint)start + (uint)length <= (uint)source.Length, "length out of range");
			return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(source), start), length);
		}
	}

	/// <summary>
	/// Provides extension methods for arrays that allow for bounds-check-free element access.
	/// </summary>
	/// <typeparam name="T">The type of elements in the array.</typeparam>
	/// <param name="array">The array to extend.</param>
	extension<T>([DisallowNull] T[] array)
	{
		/// <summary>
		/// Returns a <see cref="Span{T}"/> that begins at <paramref name="index"/> and extends to the
		/// end of <paramref name="array"/>, without performing runtime bounds checks.
		/// </summary>
		/// <param name="index">The zero-based index at which the slice begins. Must satisfy <c>0 &lt;= index &lt;= array.Length</c>.</param>
		/// <returns>A <see cref="Span{T}"/> equivalent to <c>array.AsSpan(index)</c>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<T> AsUnsafeSpan(int index)
		{
			return AsUnsafeSpan(array, index, length: array.Length - index);
		}
		/// <summary>
		/// Returns a <see cref="Span{T}"/> that begins at <paramref name="index"/> and covers
		/// <paramref name="length"/> elements, without performing runtime bounds or null-checks.
		/// </summary>
		/// <param name="index">The zero-based index at which the slice begins.</param>
		/// <param name="length">The number of elements in the slice. Must satisfy <c>index + length &lt;= array.Length</c>.</param>
		/// <returns>A <see cref="Span{T}"/> equivalent to <c>array.AsSpan(index, length)</c>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<T> AsUnsafeSpan(int index, int length)
		{
			Debug.Assert(array is not null, "array cannot be null");
			Debug.Assert((uint)index <= (uint)array.Length, "start index out of range");
			Debug.Assert((ulong)(uint)index + (uint)length <= (uint)array.Length, "length out of range");
			return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index), length);
		}

		/// <summary>
		/// Returns a reference to the element at <paramref name="index"/> in <paramref name="array"/>,
		/// without performing runtime bounds checks.
		/// </summary>
		/// <param name="index">The zero-based index of the element to reference.</param>
		/// <returns>A reference to the element at the specified index.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref T Ref(int index = 0)
		{
			Debug.Assert(array is not null, "array cannot be null");
			Debug.Assert((uint)index < (uint)array.Length, "index out of range");
			return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
		}
	}

	/// <summary>
	/// Provides extension methods for references to value types that allow for bounds-check-free element access in contiguous memory.
	/// </summary>
	/// <typeparam name="T">The type of the value type.</typeparam>
	/// <param name="src">The reference to the start of the contiguous memory.</param>
	extension<T>(ref T src) where T : struct
	{
		/// <summary>
		/// Returns a <see cref="Span{T}"/> that begins at <paramref name="src"/> and covers <paramref name="length"/>
		/// elements, without performing runtime bounds checks.
		/// </summary>
		/// <remarks>
		/// <para>A bare reference carries no length, so nothing verifies that <paramref name="length"/> elements exist
		/// starting at <paramref name="src"/>; the caller must guarantee it. Reading or writing past the real end is
		/// undefined behavior.</para>
		/// <para>The span aliases that memory rather than copying it, so writes through the span change it.</para>
		/// </remarks>
		/// <param name="length">The number of elements in the span. Must be non-negative.</param>
		/// <returns>A <see cref="Span{T}"/> of <paramref name="length"/> elements whose first element is
		/// <paramref name="src"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<T> UnsafeSpan(int length)
		{
			Debug.Assert(length >= 0, "length must be non-negative");
			return MemoryMarshal.CreateSpan(ref src, length);
		}
		/// <summary>
		/// Returns a reference to the element at <paramref name="index"/> in the memory starting at <paramref name="src"/>,
		/// without performing runtime bounds checks.
		/// </summary>
		/// <param name="index">The zero-based index of the element to reference.</param>
		/// <returns>A reference to the element at the specified index.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref T Offset(int index)
		{
			Debug.Assert(index >= 0, "index must be non-negative");
			return ref Unsafe.Add(ref src, index);
		}
	}
}
