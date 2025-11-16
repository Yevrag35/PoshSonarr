using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Json;

namespace MG.Sonarr.Next.Metadata;

/// <summary>
/// Represents a strongly-typed, sortable list of metadata items that supports tagging and JSON metadata operations.
/// </summary>
/// <remarks>MetadataList provides collection management features similar to <see cref="List{T}"/>, with
/// additional support for metadata tagging and sorting. Items in the list can be tagged using an <see
/// cref="IMetadataResolver"/>, and the list can be sorted based on the natural ordering of <typeparamref
/// name="T"/>. The class is not thread-safe; external synchronization is required if accessed
/// concurrently.</remarks>
/// <typeparam name="T">The type of elements in the list. Must implement <see cref="IComparable{T}"/> and <see
/// cref="IJsonMetadataTaggable"/>.</typeparam>
[CollectionBuilder(typeof(MetadataList), nameof(MetadataList.Create)), DebuggerDisplay("Count = {Count}")]
public sealed partial class MetadataList<T> : IList<T>, IJsonMetadataTaggable, ISortable where T : IComparable<T>, IJsonMetadataTaggable
{
	private readonly List<T> _list;

	/// <summary>
	/// Gets or sets the element at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the element to get or set. Must be greater than or equal to 0 and less than the
	/// number of elements in the collection.</param>
	/// <returns>The element at the specified index.</returns>
	public T this[int index]
	{
		get => _list[index];
		set => _list[index] = value;
	}

	/// <summary>
	/// Gets the number of elements contained in the list.
	/// </summary>
	public int Count => _list.Count;

	/// <inheritdoc/>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	bool ICollection<T>.IsReadOnly => false;

	/// <summary>
	/// Initializes a new instance of the MetadataList class with no initial capacity.
	/// </summary>
	/// <remarks>This constructor creates an empty MetadataList. Items can be added after construction
	/// as needed.</remarks>
	public MetadataList()
	{
		_list = [];
	}
	/// <summary>
	/// Initializes a new instance of the MetadataList class with the specified initial capacity.
	/// </summary>
	/// <param name="capacity">The number of elements that the list can initially contain. Must be non-negative.</param>
	public MetadataList(int capacity)
	{
		_list = new(capacity);
	}
	/// <summary>
	/// Initializes a new instance of the MetadataList class that contains elements copied from the specified
	/// collection.
	/// </summary>
	/// <param name="items">The collection of items to copy into the list.</param>
	public MetadataList(IEnumerable<T>? items)
	{
		if (items is null)
		{
			_list = [];
			return;
		}

		_list = [.. items];
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="MetadataList{T}"/> class with the specified values.
	/// </summary>
	/// <param name="values">A read-only span containing the values to include in the list. The contents are copied; subsequent changes to the
	/// span do not affect the list.</param>
	internal MetadataList(params ReadOnlySpan<T> values)
	{
		_list = [.. values];
	}

	/// <summary>
	/// Adds the specified item to the collection if it is not null.
	/// </summary>
	/// <param name="item">The item to add to the collection. If <paramref name="item"/> is null, it will not be added.</param>
	public void Add(T item)
	{
		if (item is not null)
		{
			_list.Add(item);
		}
	}

	/// <summary>
	/// Adds the elements of the specified collection to the end of the list.
	/// </summary>
	/// <param name="collection">The collection whose elements should be added to the list. If <paramref name="collection"/> is <see
	/// langword="null"/>, no elements are added.</param>
	public void AddRange(IEnumerable<T>? collection)
	{
		if (collection is not null)
		{
			_list.AddRange(collection);
		}
	}
	
	/// <summary>
	/// Removes all items from the collection.
	/// </summary>
	/// <remarks>After calling this method, the collection will be empty. This operation does not
	/// modify the capacity of the underlying storage.</remarks>
	public void Clear()
	{
		_list.Clear();
	}
	/// <summary>
	/// Determines whether the collection contains a specific element.
	/// </summary>
	/// <param name="item">The element to locate in the collection. The value can be null for reference types.</param>
	/// <returns>true if the element is found in the collection; otherwise, false.</returns>
	public bool Contains(T item)
	{
		_list.Contains(item);
		foreach (T value in this.AsSpan())
		{
			if (_list.IndexOf(item) != -1 || value.CompareTo(item) == 0)
				return true;
		}

		return false;
	}
	/// <summary>
	/// Copies the elements of the collection to the specified array, starting at the given array index.
	/// </summary>
	/// <remarks>The elements are copied in the same order as they are stored in the collection. If
	/// the array is not large enough to accommodate the copied elements, an exception will be thrown.</remarks>
	/// <param name="array">The destination array that will receive the copied elements. Must be large enough to contain the elements
	/// from the specified index onward.</param>
	/// <param name="index">The zero-based index in the destination array at which copying begins.</param>
	public void CopyTo(T[] array, int index)
	{
		_list.CopyTo(array, index);
	}
	public T? Find(Predicate<T> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		return _list.Find(predicate);
	}

	public int IndexOf(T item)
	{
		return _list.IndexOf(item);
	}
	public void Insert(int index, T item)
	{
		_list.Insert(index, item);
	}
	public bool Remove(T item)
	{
		return _list.Remove(item);
	}
	public void RemoveAt(int index)
	{
		_list.RemoveAt(index);
	}
	/// <summary>
	/// Removes all elements from the collection that match the specified predicate, using the provided arguments.
	/// </summary>
	/// <typeparam name="TArg1">The type of the first argument passed to the predicate.</typeparam>
	/// <typeparam name="TArg2">The type of the second argument passed to the predicate.</typeparam>
	/// <param name="arg1">The first argument to pass to the predicate for each element.</param>
	/// <param name="arg2">The second argument to pass to the predicate for each element.</param>
	/// <param name="predicate">A function pointer that determines whether an element should be removed. The function receives the element,
	/// <paramref name="arg1"/>, and <paramref name="arg2"/> as parameters, and returns <see langword="true"/> to
	/// remove the element; otherwise, <see langword="false"/>.</param>
	/// <exception cref="ArgumentException"><paramref name="predicate"/> is not a valid function pointer.</exception>
	internal void RemoveAll<TArg1, TArg2>(TArg1 arg1, TArg2 arg2, FnPtr<T, TArg1, TArg2, bool> predicate)
		where TArg1 : allows ref struct
		where TArg2 : allows ref struct
	{
		FnPtr.ThrowIfInvalid(predicate);

		int listCount = _list.Count;
		if (listCount == 0)
			return;

		for (int i = listCount - 1; i >= 0; i--)
		{
			if (predicate.Invoke(_list[i], arg1, arg2))
			{
				_list.RemoveAt(i);
			}
		}
	}

	/// <inheritdoc/>
	public void SetTag(IMetadataResolver resolver)
	{
		foreach (T item in CollectionsMarshal.AsSpan(_list))
		{
			item.SetTag(resolver);
		}
	}

	/// <inheritdoc cref="List{T}.Sort()"/>
	public void Sort()
	{
		_list.Sort();
	}

	/// <summary>
	/// Attempts to find an item in the collection that matches the specified predicate and state.
	/// </summary>
	/// <remarks>This method performs a linear search over the collection. The predicate is invoked
	/// for each item, passing the current item and the provided state. If multiple items match, only the first is
	/// returned. The method does not modify the collection.</remarks>
	/// <typeparam name="TState">The type of the state object passed to the predicate for evaluation.</typeparam>
	/// <param name="state">An object representing state information to be supplied to the predicate for each item.</param>
	/// <param name="predicate">A pointer to a function that determines whether an item matches the desired condition, given the item and
	/// the state. The function should return <see langword="true"/> to indicate a match; otherwise, <see
	/// langword="false"/>.</param>
	/// <param name="result">When this method returns, contains the first item that matches the predicate if found; otherwise, the
	/// default value for type <c>T</c>. This parameter is passed uninitialized.</param>
	/// <returns><see langword="true"/> if a matching item is found; otherwise, <see langword="false"/>.</returns>
	internal bool TryFind<TState>(TState state, FnPtr<T, TState, bool> predicate, [NotNullWhen(true)] out T? result)
	{
		foreach (T item in CollectionsMarshal.AsSpan(_list))
		{
			if (predicate.Invoke(item, state))
			{
				result = item;
				return true;
			}
		}

		result = default;
		return false;
	}
}

