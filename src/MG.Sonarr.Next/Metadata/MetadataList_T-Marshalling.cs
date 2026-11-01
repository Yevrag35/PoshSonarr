using MG.Sonarr.Next.Json;

namespace MG.Sonarr.Next.Metadata;

public sealed partial class MetadataList<T>
{
	/// <summary>
	/// Adds the elements of a span to the end of the list.
	/// </summary>
	/// <remarks>
	/// The elements are copied straight into the list's backing array, growing it first when they don't fit.
	/// </remarks>
	/// <param name="values">The elements to add.</param>
	public void AddRange(params ReadOnlySpan<T> values)
	{
		const int defCap = 4;

		if (!values.IsEmpty)
		{
			T[] array = ListMarshal.GetBackingArray(_list, out int size);
			if (array.Length - size < values.Length)
			{
				int newCap = array.Length == 0 ? defCap : 2 * array.Length;
				// Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
				if ((uint)newCap > (uint)Array.MaxLength) newCap = Array.MaxLength;

				if (newCap < values.Length) newCap = values.Length;

				_list.Capacity = newCap;
			}

			ListMarshal.AddTo(_list, values);
		}
	}

	/// <summary>
	/// Returns a read-only span over the list's elements.
	/// </summary>
	/// <remarks>
	/// The span views the list's backing array. A change to the list's contents shows in the span, but an addition
	/// that grows the list moves its elements to a new array, which the span doesn't follow, so get a new span after
	/// changing the list.
	/// </remarks>
	/// <returns>A span over the list's <see cref="Count"/> elements, or an empty span for an empty list.</returns>
	public ReadOnlySpan<T> AsSpan()
	{
		return ListMarshal.GetBackingSpan(_list);
	}
}

/// <summary>
/// Provides access to the private fields of a <see cref="List{T}"/>, through a class with the same layout.
/// </summary>
file static class ListMarshal
{
	/// <summary>
	/// Checks that <see cref="List{T}"/>'s instance fields have the layout that <see cref="ListView{T}"/> copies.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// Thrown when <see cref="List{T}"/>'s instance fields don't match the expected layout.
	/// </exception>
	static ListMarshal()
	{
		FieldInfo[] fields = typeof(List<>).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
		if (!(fields.Length == 3
			&& fields[0].Name == "_items" && fields[0].FieldType.IsArray
			&& fields[1].Name == "_size" && typeof(int).Equals(fields[1].FieldType)
			&& fields[2].Name == "_version") && typeof(int).Equals(fields[2].FieldType))
		{
			throw new InvalidOperationException("List has changed its internal structure; cannot continue.");
		}
	}

	/// <summary>
	/// Copies values to the end of a list whose backing array has room for them.
	/// </summary>
	/// <typeparam name="T">The type of the list's elements.</typeparam>
	/// <param name="list">The list to add to, which has room for <paramref name="values"/>.</param>
	/// <param name="values">The values to add, at least one.</param>
	/// <exception cref="ArgumentException">Thrown when the backing array has no room for <paramref name="values"/>.</exception>
	internal static void AddTo<T>(List<T> list, ReadOnlySpan<T> values) where T : IComparable<T>, IJsonMetadataTaggable
	{
		Debug.Assert(!values.IsEmpty);
		var view = Unsafe.As<ListView<T>>(list);
		Debug.Assert(view._items.Length - view._size >= values.Length);

		values.CopyTo(view._items.AsSpan(view._size));
		view._size += values.Length;
		view._version++;
	}

	/// <summary>
	/// Gets a reference to the first element of a list's backing array.
	/// </summary>
	/// <typeparam name="T">The type of the list's elements.</typeparam>
	/// <param name="list">The list.</param>
	/// <returns>A reference to the array's first element, which may lie past its end when the array is empty.</returns>
	private static ref T GetListReference<T>(List<T> list) where T : IComparable<T>, IJsonMetadataTaggable
	{
		ListView<T> view = Unsafe.As<ListView<T>>(list);
		return ref MemoryMarshal.GetArrayDataReference(view._items);
	}

	/// <summary>
	/// Gets a list's backing array and its count.
	/// </summary>
	/// <typeparam name="T">The type of the list's elements.</typeparam>
	/// <param name="list">The list.</param>
	/// <param name="size">When this method returns, the list's count.</param>
	/// <returns>The list's backing array, whose length is its capacity.</returns>
	internal static T[] GetBackingArray<T>(List<T> list, out int size) where T : IComparable<T>, IJsonMetadataTaggable
	{
		ListView<T> view = Unsafe.As<ListView<T>>(list);
		size = view._size;
		return view._items;
	}

	/// <summary>
	/// Gets a span over a list's elements.
	/// </summary>
	/// <typeparam name="T">The type of the list's elements.</typeparam>
	/// <param name="list">The list.</param>
	/// <returns>A span over the first <see cref="List{T}.Count"/> elements of the backing array.</returns>
	internal static Span<T> GetBackingSpan<T>(List<T> list) where T : IComparable<T>, IJsonMetadataTaggable
	{
		ListView<T> view = Unsafe.As<ListView<T>>(list);
		int count = view._size;

		return count > 0
			? MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(view._items), count)
			: [];
	}

	/// <summary>
	/// Sets a list's count without clearing or changing its elements.
	/// </summary>
	/// <typeparam name="T">The type of the list's elements.</typeparam>
	/// <param name="list">The list.</param>
	/// <param name="size">The new count, which is at most the list's capacity.</param>
	internal static void SetCount<T>(List<T> list, int size) where T : IComparable<T>, IJsonMetadataTaggable
	{
		ListView<T> view = Unsafe.As<ListView<T>>(list);
		Debug.Assert(size <= view._items.Length, "The size being set should be less than or equal to the array length.");
		view._size = size;
	}

	/// <summary>
	/// Represents the layout of <see cref="List{T}"/>'s instance fields, which a list is reinterpreted as.
	/// </summary>
	/// <typeparam name="T">The type of the list's elements.</typeparam>
	private sealed class ListView<T> where T : IComparable<T>, IJsonMetadataTaggable
	{
		/// <summary>
		/// The list's backing array.
		/// </summary>
		internal T[] _items = null!;
		/// <summary>
		/// The list's count.
		/// </summary>
		internal int _size;
		/// <summary>
		/// The list's version, which its enumerator checks to detect a change.
		/// </summary>
		internal int _version;
	}
}
