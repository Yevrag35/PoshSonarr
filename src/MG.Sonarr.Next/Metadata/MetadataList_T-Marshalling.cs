using MG.Sonarr.Next.Json;
using System.Reflection;

namespace MG.Sonarr.Next.Metadata;

public sealed partial class MetadataList<T>
{
	/// <summary>Adds the elements of the specified span to the end of the <see cref="MetadataList{T}"/>.</summary>
	/// <param name="values">The span whose elements should be added to the end of the <see cref="MetadataList{T}"/>.</param>
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
	/// Returns a read-only span over the elements in the collection.
	/// </summary>
	/// <remarks>The returned span reflects the current state of the underlying collection. Modifying
	/// the collection after obtaining the span may invalidate the span or result in undefined behavior. This method
	/// is intended for performance-critical scenarios where direct access to the underlying data is
	/// required.</remarks>
	/// <returns>A <see cref="ReadOnlySpan{T}"/> that provides a read-only view of the collection's elements.</returns>
	public ReadOnlySpan<T> AsSpan()
	{
		return ListMarshal.GetBackingSpan(_list);
	}
}

file static class ListMarshal
{
	/// <summary>
	/// Initializes static data for the ListMarshal class and verifies the internal structure of the generic <see cref="List{T}"/> type.
	/// </summary>
	/// <remarks>This static constructor inspects the private fields of <see cref="List{T}"/> to ensure compatibility with its
	/// internal representation. If the structure of <see cref="List{T}"/> changes in future .NET versions, this check helps prevent
	/// incorrect or unsafe operations that rely on the current layout.</remarks>
	/// <exception cref="InvalidOperationException">Thrown if the internal structure of <see cref="List{T}"/> does not match the expected layout, indicating that the implementation
	/// has changed and the class cannot operate safely.</exception>
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

	internal static void AddTo<T>(List<T> list, ReadOnlySpan<T> values) where T : IComparable<T>, IJsonMetadataTaggable
	{
		Debug.Assert(!values.IsEmpty);
		var view = Unsafe.As<ListView<T>>(list);
		Debug.Assert(view._items.Length - view._size >= values.Length);

		values.CopyTo(view._items.AsSpan(view._size));
		view._size += values.Length;
		view._version++;
	}

	private static ref T GetListReference<T>(List<T> list) where T : IComparable<T>, IJsonMetadataTaggable
	{
		ListView<T> view = Unsafe.As<ListView<T>>(list);
		return ref MemoryMarshal.GetArrayDataReference(view._items);
	}

	internal static T[] GetBackingArray<T>(List<T> list, out int size) where T : IComparable<T>, IJsonMetadataTaggable
	{
		ListView<T> view = Unsafe.As<ListView<T>>(list);
		size = view._size;
		return view._items;
	}

	internal static Span<T> GetBackingSpan<T>(List<T> list) where T : IComparable<T>, IJsonMetadataTaggable
	{
		ListView<T> view = Unsafe.As<ListView<T>>(list);
		int count = view._size;

		return count > 0
			? MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(view._items), count)
			: [];
	}

	internal static void SetCount<T>(List<T> list, int size) where T : IComparable<T>, IJsonMetadataTaggable
	{
		ListView<T> view = Unsafe.As<ListView<T>>(list);
		Debug.Assert(size <= view._items.Length, "The size being set should be less than or equal to the array length.");
		view._size = size;
	}

	private sealed class ListView<T> where T : IComparable<T>, IJsonMetadataTaggable
	{
		internal T[] _items = null!;
		internal int _size;
		internal int _version;
	}
}
