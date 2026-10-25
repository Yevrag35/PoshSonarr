using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Buffers.Intrinsics;

namespace MG.Sonarr.Next.Collections.Intrinsics;

/// <summary>
/// Provides advanced utilities for accessing and manipulating the internal data of generic <see cref="List{T}"/> instances. Intended
/// for scenarios that require high-performance operations or interoperability with low-level memory constructs.
/// </summary>
/// <remarks>The <see cref="ListMarshal"/> class exposes methods that allow direct access to the underlying storage of <see cref="List{T}"/>
/// objects, such as obtaining a read-only span over the list's elements. These operations rely on the internal
/// structure of <see cref="List{T}"/> and may not be compatible with future versions of .NET if the implementation changes. Use with
/// caution, as improper use can lead to undefined behavior or runtime exceptions. This class is primarily intended for
/// advanced scenarios where performance is critical and the risks are understood.</remarks>
internal static class ListMarshal
{
	/// <summary>
	/// Appends the elements of <paramref name="values"/> to the end of the specified list.
	/// </summary>
	/// <remarks>
	/// <para>This method writes directly into the list's backing array and updates the list size and modification version manually.</para>
	/// <para><b>Performance:</b> This method avoids per-element <see cref="List{T}.Add(T)"/> overhead and copies the source span in one bulk operation.</para>
	/// </remarks>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	/// <param name="list">The list to extend. This value must not be <see langword="null"/>; it is asserted in debug builds
	/// and unvalidated in release builds.</param>
	/// <param name="values">The elements to append to <paramref name="list"/>.</param>
	/// <exception cref="OverflowException">Thrown when appending <paramref name="values"/> would raise the count of
	/// <paramref name="list"/> past <see cref="int.MaxValue"/>.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AddRange<T>(List<T> list, params ReadOnlySpan<T> values)
	{
		Debug.Assert(list is not null, "The caller is expected to short-circuit a null list.");
		if (values.IsEmpty) return;

		values.UnsafeCopy(ReserveTail(list, values.Length, out int size));
		CommitTail(list, size, values.Length);
	}

	/// <summary>
	/// Converts the elements of <paramref name="values"/> with <paramref name="conversion"/> and appends the results to
	/// the end of the specified list.
	/// </summary>
	/// <remarks>
	/// <para>The converted elements are written straight into the list's backing array, so no intermediate buffer is
	/// allocated and the list is resized at most once no matter how many elements <paramref name="values"/> holds.</para>
	/// <para>The element count and modification version are published only after every element has been converted, so a
	/// <paramref name="conversion"/> that throws part-way leaves <paramref name="list"/> exactly as it was found, apart
	/// from any capacity the append reserved. <paramref name="conversion"/> must not mutate <paramref name="list"/>: the
	/// destination is captured before the first call, and an append or a capacity change made from inside the conversion
	/// abandons that array while the loop is still writing to it.</para>
	/// <para>
	/// If <paramref name="conversion"/> is a <see langword="null"/> pointer, the method returns without modifying <paramref name="list"/>.
	/// </para>
	/// </remarks>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	/// <typeparam name="TOther">The type of elements in <paramref name="values"/>.</typeparam>
	/// <param name="list">The list to extend. This value must not be <see langword="null"/>; it is asserted in debug builds
	/// and unvalidated in release builds.</param>
	/// <param name="conversion">A pointer to the function that converts an element of type <typeparamref name="TOther"/> to
	/// <typeparamref name="T"/>.</param>
	/// <param name="values">The elements to convert and append to <paramref name="list"/>.</param>
	/// <exception cref="OverflowException">Thrown when appending <paramref name="values"/> would raise the count of
	/// <paramref name="list"/> past <see cref="int.MaxValue"/>.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AddRange<T, TOther>(List<T> list, FnPtr<TOther, T> conversion, params ReadOnlySpan<TOther> values)
	{
		Assert.NotNull(list);
		if (!conversion.IsValid || values.IsEmpty) return;

		int length = values.Length;
		Span<T> section = ReserveTail(list, length, out int size);

		ref TOther source = ref MemoryMarshal.GetReference(values);
		ref T destination = ref MemoryMarshal.GetReference(section);

		for (nuint i = 0; i < (uint)length; i++)
		{
			Unsafe.Add(ref destination, i) = conversion.Invoke(Unsafe.Add(ref source, i));
		}

		CommitTail(list, size, length);
	}

	/// <summary>
	/// Grows the specified list if needed and returns a writable span over the <paramref name="count"/> slots that follow
	/// its last element.
	/// </summary>
	/// <remarks>The returned span covers backing-array slots that the list does not yet consider part of its contents.
	/// The caller fills the span and then publishes it with <see cref="CommitTail{T}(List{T}, int, int)"/>; until that
	/// call, <paramref name="list"/> reports its original count and modification version.</remarks>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	/// <param name="list">The list to reserve room in.</param>
	/// <param name="count">The number of slots to reserve. Must be greater than zero.</param>
	/// <param name="size">Outputs the count of <paramref name="list"/> as it stood before the reservation.</param>
	/// <returns>A span over the reserved slots, in order.</returns>
	/// <exception cref="OverflowException">Thrown when reserving <paramref name="count"/> slots would raise the count of
	/// <paramref name="list"/> past <see cref="int.MaxValue"/>.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Span<T> ReserveTail<T>(List<T> list, int count, out int size)
	{
		Debug.Assert(count > 0, "The caller is expected to short-circuit an empty append.");

		size = View<T>.Size(list);
		ref T[] array = ref View<T>.Items(list);

		if (array.Length - size < count)
		{
			CheckAndGrow(list, count, size);
		}

		// Read through the ref after the growth check: Grow replaces the _items field outright, so a by-value
		// capture taken above would address the abandoned array.
		return MemoryMarshal.CreateSpan(
			ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), size), count);
	}

	/// <summary>
	/// Grows the backing array of the specified list to hold <paramref name="count"/> elements beyond its current count.
	/// </summary>
	/// <remarks>This is the cold half of <see cref="ReserveTail{T}(List{T}, int, out int)"/>, kept out of line so the
	/// capacity test that guards it inlines into each caller without carrying the growth path with it.</remarks>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	/// <param name="list">The list to grow.</param>
	/// <param name="count">The number of elements about to be appended.</param>
	/// <param name="size">The current count of <paramref name="list"/>.</param>
	/// <exception cref="OverflowException">Thrown when <paramref name="size"/> plus <paramref name="count"/> exceeds
	/// <see cref="int.MaxValue"/>.</exception>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void CheckAndGrow<T>(List<T> list, int count, int size)
	{
		int newCount = checked(size + count);

		// Grow, not the public EnsureCapacity: EnsureCapacity bumps _version itself when it reallocates,
		// which would report two modifications for a single append. Grow is internal to List<T>, so a rename
		// upstream surfaces here as a MissingMethodException on first call rather than a compile error.
		View<T>.Grow(list, newCount);
	}

	/// <summary>
	/// Publishes slots reserved by <see cref="ReserveTail{T}(List{T}, int, out int)"/> as elements of the specified list.
	/// </summary>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	/// <param name="list">The list whose count and modification version are updated.</param>
	/// <param name="size">The count of <paramref name="list"/> as it stood before the reservation.</param>
	/// <param name="count">The number of reserved slots to publish.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void CommitTail<T>(List<T> list, int size, int count)
	{
		View<T>.Size(list) = size + count;

		View<T>.Version(list)++; // Increment the version to reflect the modification.
	}

	/// <summary>
	/// Returns a read-only span that represents the elements of the specified list.
	/// </summary>
	/// <remarks>The returned span reflects the current contents of the list at the time of the call. Modifying the
	/// list after obtaining the span may invalidate the span or result in undefined behavior. The span does not allocate
	/// additional memory.</remarks>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	/// <param name="list">The list whose elements are to be represented as a read-only span. Cannot be null.</param>
	/// <returns>A read-only span containing the elements of the list in order.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<T> AsReadOnlySpan<T>(List<T> list)
	{
		int size = View<T>.Size(list);
		return size > 0
			? MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(View<T>.Items(list)), size)
			: [];
	}
	/// <summary>
	/// Returns a read-only span that represents the elements of the specified list.
	/// </summary>
	/// <remarks>The returned span reflects the current contents of the list at the time of the call. Modifying the
	/// list after obtaining the span may invalidate the span or result in undefined behavior. The span does not allocate
	/// additional memory.</remarks>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	/// <param name="list">The list whose elements are to be represented as a read-only span. Cannot be null.</param>
	/// <returns>A read-only span containing the elements of the list in order.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<T> AsSpan<T>(List<T> list)
	{
		int size = View<T>.Size(list);
		return size > 0
			? MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(View<T>.Items(list)), size)
			: [];
	}

	/// <summary>
	/// Creates a new <see cref="List{T}"/> pre-populated with the elements of <paramref name="values"/> by writing them
	/// directly into the list's backing array.
	/// </summary>
	/// <remarks>
	/// <para>The returned list is an ordinary, fully independent <see cref="List{T}"/>. Its elements are copied out of
	/// <paramref name="values"/>, so the two never alias, and indexing, enumerating, mutating, and growing all behave
	/// exactly as they do on a list produced by a public constructor.</para>
	/// <para><b>Compared to traditional construction.</b> Building a list from a span through the public surface always
	/// costs something, and each available route pays it differently:</para>
	/// <list type="bullet">
	/// <item><description><c>new List&lt;T&gt;(values.ToArray())</c> allocates twice, a throwaway array plus the list's
	/// own, and copies the elements twice.</description></item>
	/// <item><description><c>new List&lt;T&gt;(values.Length)</c> followed by a loop of <see cref="List{T}.Add(T)"/>
	/// allocates once, but pays a capacity check and a modification-version bump per element and never lowers to a bulk
	/// copy.</description></item>
	/// <item><description><see cref="CollectionExtensions.AddRange{T}(List{T}, ReadOnlySpan{T})"/> allocates once and
	/// does copy in bulk, but re-checks capacity the caller has already sized correctly and bumps the modification
	/// version.</description></item>
	/// <item><description><see cref="CollectionsMarshal.SetCount{T}(List{T}, int)"/> paired with
	/// <see cref="CollectionsMarshal.AsSpan{T}(List{T})"/> comes closest, but still runs a capacity branch, bumps the
	/// modification version, and performs a length-checked copy.</description></item>
	/// </list>
	/// <para>This method instead sizes the backing array to <paramref name="values"/> exactly, copies the span into it in
	/// one unchecked bulk operation, and publishes the element count by writing the size field directly, for a total of
	/// one allocation and one copy with no per-element bookkeeping. Leaving the modification version untouched also makes
	/// the result indistinguishable from a freshly constructed list, which no route that fills a list after constructing
	/// it can offer, the collection expression <c>[.. values]</c> included: a list built that way already reports one
	/// modification before the caller ever sees it.</para>
	/// <para>Two consequences are worth planning around. <see cref="List{T}.Capacity"/> equals <see cref="List{T}.Count"/>
	/// on return, with no headroom, so the first <see cref="List{T}.Add(T)"/> reallocates and copies; call
	/// <see cref="List{T}.EnsureCapacity(int)"/> first when the list is known to be about to grow. And because
	/// <paramref name="values"/> is a <see langword="params"/> span, passing a <see langword="null"/> array produces an
	/// empty list instead of throwing, unlike the <see cref="List{T}"/> constructor that accepts an
	/// <see cref="IEnumerable{T}"/>.</para>
	/// </remarks>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	/// <param name="values">The elements to copy into the new list, in order. May be empty.</param>
	/// <returns>A new <see cref="List{T}"/> containing the elements of <paramref name="values"/>.</returns>
	public static List<T> Create<T>(params ReadOnlySpan<T> values)
	{
		List<T> list = new(values.Length);
		if (!values.IsEmpty)
		{
			Span<T> span = MemoryMarshal.CreateSpan(
				ref MemoryMarshal.GetArrayDataReference(GetRawArray(list)),
				values.Length);

			values.UnsafeCopy(span);
			SetCount(list, values.Length);
		}

		return list;
	}

	/// <summary>
	/// Creates a new <see cref="List{T}"/> with room for at least <paramref name="capacity"/> elements, pre-populated with
	/// the elements of <paramref name="values"/> by writing them directly into the list's backing array.
	/// </summary>
	/// <remarks>
	/// <para>This is <see cref="Create{T}(ReadOnlySpan{T})"/> with reserved headroom, and shares its construction
	/// semantics: the elements are copied rather than aliased, and the returned list is indistinguishable from a freshly
	/// constructed one. See that overload for how writing the backing array directly compares to building a list through
	/// the public surface. What differs here is that <see cref="List{T}.Capacity"/> comes back at
	/// <paramref name="capacity"/> instead of at <see cref="List{T}.Count"/>, so appends up to that point do not
	/// reallocate.</para>
	/// <para><paramref name="capacity"/> is a floor rather than an exact size, and never truncates: a value below the
	/// length of <paramref name="values"/> is raised to that length, so every element is always copied. A negative value
	/// is clamped the same way instead of throwing, which differs from the <see cref="List{T}"/> constructor that takes a
	/// capacity. When the effective capacity works out to zero, the list comes back empty and no array is allocated at
	/// all.</para>
	/// <para>Slots between <see cref="List{T}.Count"/> and <see cref="List{T}.Capacity"/> hold <c>default(T)</c>, so the
	/// reserved region carries no stale references and behaves exactly like headroom obtained from
	/// <see cref="List{T}.EnsureCapacity(int)"/>.</para>
	/// </remarks>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	/// <param name="capacity">The minimum number of elements the new list can hold before it reallocates. Values below the
	/// length of <paramref name="values"/>, negative ones included, are raised to that length.</param>
	/// <param name="values">The elements to copy into the new list, in order. May be empty, in which case the list is
	/// returned empty with the requested capacity reserved.</param>
	/// <returns>A new <see cref="List{T}"/> containing the elements of <paramref name="values"/>, with room for at least
	/// <paramref name="capacity"/> elements.</returns>
	[SuppressMessage("Style", "IDE0028")]
	public static List<T> CreateWithCapacity<T>(int capacity, params ReadOnlySpan<T> values)
	{
		List<T> list = new();
		capacity = Math.Max(capacity, values.Length);
		if (capacity == 0) return list;

		T[] array = new T[capacity];
		View<T>.Items(list) = array;

		if (!values.IsEmpty)
		{
			Span<T> span = MemoryMarshal.CreateSpan(
				ref MemoryMarshal.GetArrayDataReference(array),
				array.Length);

			values.UnsafeCopy(span);
			View<T>.Size(list) = values.Length;
		}

		return list;
	}

	/// <summary>
	/// Returns the backing array of the specified list wrapped as an <see cref="ArraySlice{T}"/> over its populated range.
	/// </summary>
	/// <remarks>The slice references the list's internal storage and does not allocate. Modifying the list after this call may invalidate the slice or result in undefined behavior.</remarks>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	/// <param name="list">The list whose backing array is wrapped. Cannot be null.</param>
	/// <returns>An <see cref="ArraySlice{T}"/> spanning the elements of <paramref name="list"/> in order.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ArraySlice<T> GetArray<T>(List<T> list)
	{
		return ArraySliceMarshal.Create(View<T>.Items(list), 0, View<T>.Size(list));
	}
	/// <summary>
	/// Returns the backing array of the specified list wrapped as an <see cref="ArraySlice{T}"/> over its populated range.
	/// </summary>
	/// <remarks>The slice references the list's internal storage and does not allocate. Modifying the list after this call may invalidate the slice or result in undefined behavior.</remarks>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	/// <param name="list">The list whose backing array is wrapped. Cannot be null.</param>
	/// <param name="version">Outputs the current version of the list, which can be used to detect modifications.</param>
	/// <returns>An <see cref="ArraySlice{T}"/> spanning the elements of <paramref name="list"/> in order.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ArraySlice<T> GetArray<T>(List<T> list, out int version)
	{
		version = View<T>.Version(list);
		return ArraySliceMarshal.Create(View<T>.Items(list), 0, View<T>.Size(list));
	}

	/// <summary>
	/// Returns the raw backing array of the specified list. This method provides direct access to the internal array used by the list, which may contain uninitialized elements beyond the current count of the list.
	/// </summary>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	/// <param name="list">The list whose raw backing array is to be returned.</param>
	/// <returns>The raw backing array of the specified list.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static T[] GetRawArray<T>(List<T> list)
	{
		return View<T>.Items(list);
	}

	/// <summary>
	/// Validates that the specified list has not been modified since the provided version. If the list's current version does not match the expected version, an <see cref="InvalidOperationException"/> is thrown.
	/// </summary>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	/// <param name="source">The list to validate.</param>
	/// <param name="expectedVersion">The expected version of the list.</param>
	/// <exception cref="InvalidOperationException">Thrown when the list's version does not match the expected version.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidateVersion<T>(List<T> source, int expectedVersion)
	{
		int version = View<T>.Version(source);
		if (expectedVersion != version)
		{
			ThrowInvalidVersion(expectedVersion, version);
		}
	}

	/// <summary>
	/// Throws an <see cref="InvalidOperationException"/> for a list version mismatch.
	/// </summary>
	/// <param name="expected">The version that the caller expected to observe.</param>
	/// <param name="actual">The version currently stored by the list.</param>
	/// <exception cref="InvalidOperationException">Thrown unconditionally.</exception>
	[DoesNotReturn, DebuggerStepThrough, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowInvalidVersion(int expected, int actual)
	{
		throw new InvalidOperationException(
			message: $"Collection was modified; expected version {expected}, but found {actual}.");
	}

	///// <summary>
	///// Returns a tuple containing references to the first elements of two lists, allowing for direct manipulation of their underlying arrays.
	///// </summary>
	///// <remarks>
	///// This method ensures that the target list has sufficient capacity to hold the specified count of elements.
	///// </remarks>
	///// <typeparam name="T">The type of elements in the source list.</typeparam>
	///// <typeparam name="TOther">The type of elements in the target list.</typeparam>
	///// <param name="source">The source list.</param>
	///// <param name="target">The target list.</param>
	///// <param name="count">The number of elements to consider.</param>
	///// <returns>A tuple containing references to the first elements of the source and target lists.</returns>
	//[MethodImpl(MethodImplOptions.AggressiveInlining)]
	//internal static ByRefTuple<T, TOther> GetTwoArrayFirstRef<T, TOther>(List<T> source, List<TOther> target, int count)
	//{
	//	Debug.Assert(count == source.Count, "The count is intended to match the source list count.");
	//	if (target.Capacity < count)
	//	{
	//		_ = target.EnsureCapacity(count);
	//	}

	//	ref T fOfT = ref MemoryMarshal.GetArrayDataReference(View<T>.Items(source));
	//	ref TOther fOfTOther = ref MemoryMarshal.GetArrayDataReference(View<TOther>.Items(target));
	//	return new(ref fOfT, ref fOfTOther);
	//}

	/// <summary>
	/// Sets the count of the specified list to the provided size. This method directly modifies the internal size field of the list, which can lead to undefined behavior if not used carefully.
	/// </summary>
	/// <remarks>
	/// The new size must be less than or equal to the length of the backing array.
	/// </remarks>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	/// <param name="list">The list whose count is to be set.</param>
	/// <param name="size">The new size of the list.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SetCount<T>(List<T> list, int size)
	{
		Debug.Assert(size <= View<T>.Items(list).Length, "The size being set should be less than or equal to the array length.");
		View<T>.Size(list) = size;
	}

	/// <summary>
	/// Provides a view into the internal fields of a generic <see cref="List{T}"/> instance, allowing access to its backing array, size, and version.
	/// </summary>
	/// <remarks>
	/// This class is intended for advanced scenarios where direct manipulation of the list's internal state is required.
	/// </remarks>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	private static class View<T>
	{
		///// <summary>
		///// Returns references to the backing array and logical size of the specified list.
		///// </summary>
		///// <param name="list">The list to inspect.</param>
		///// <returns>A tuple that exposes references to the list backing array and size field.</returns>
		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//internal static ByRefTuple<T[], int> GetItemsAndSize(List<T> list)
		//{
		//	ref T[] array = ref Items(list);
		//	ref int size = ref Size(list);
		//	return new(ref array, ref size);
		//}

		/// <summary>
		/// Returns a reference to the backing array field of the specified list.
		/// </summary>
		/// <param name="list">The list to inspect.</param>
		/// <returns>A reference to the internal array that stores the list elements.</returns>
		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_items")]
		internal static extern
#if NET11_0_OR_GREATER
			safe
#endif
			ref T[] Items(List<T> list);

		/// <summary>
		/// Returns a reference to the size field of the specified list.
		/// </summary>
		/// <param name="list">The list to inspect.</param>
		/// <returns>A reference to the internal count field.</returns>
		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_size")]
		internal static extern
#if NET11_0_OR_GREATER
			safe
#endif
			ref int Size(List<T> list);

		/// <summary>
		/// Returns a reference to the version field of the specified list.
		/// </summary>
		/// <param name="list">The list to inspect.</param>
		/// <returns>A reference to the internal modification version field.</returns>
		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_version")]
		internal static extern
#if NET11_0_OR_GREATER
			safe
#endif
			ref int Version(List<T> list);

		/// <summary>
		/// Grows the backing storage of the specified list to support the requested capacity.
		/// </summary>
		/// <param name="list">The list to grow.</param>
		/// <param name="capacity">The minimum capacity to ensure.</param>
		[UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(Grow))]
		internal static extern
#if NET11_0_OR_GREATER
			safe
#endif
			void Grow(List<T> list, int capacity);
	}

	private static class Assert
	{
		[Conditional("DEBUG")]
		internal static void NotNull<T>(T? value) where T : class
		{
			if (value is null)
			{
				throw new ArgumentNullException(nameof(value), "The argument cannot be null.");
			}
		}
	}
}
