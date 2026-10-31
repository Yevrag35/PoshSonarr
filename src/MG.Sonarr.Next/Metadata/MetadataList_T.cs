using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Json;

namespace MG.Sonarr.Next.Metadata;

/// <summary>
/// Represents a list of Sonarr objects that can be tagged and sorted as a whole.
/// </summary>
/// <remarks>
/// <para>The list works like a <see cref="List{T}"/>. <see cref="SetTag(IMetadataResolver)"/> tags each element,
/// and <see cref="Sort"/> sorts the elements by <typeparamref name="T"/>'s own order, which is the order that a
/// cmdlet writes them in.</para>
/// <para>The list isn't thread-safe.</para>
/// </remarks>
/// <typeparam name="T">The type of the list's elements.</typeparam>
[CollectionBuilder(typeof(MetadataList), nameof(MetadataList.Create)), DebuggerDisplay("Count = {Count}")]
public sealed partial class MetadataList<T> : IList<T>, IJsonMetadataTaggable, ISortable where T : IComparable<T>, IJsonMetadataTaggable
{
	/// <summary>
	/// The list that holds the elements.
	/// </summary>
	private readonly List<T> _list;

	/// <summary>
	/// Gets or sets the element at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the element.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="index"/> is less than 0, or is equal to or greater than <see cref="Count"/>.
	/// </exception>
	/// <value>The element at <paramref name="index"/>.</value>
	public T this[int index]
	{
		get => _list[index];
		set => _list[index] = value;
	}

	/// <summary>
	/// Gets the number of elements in the list.
	/// </summary>
	/// <value>The number of elements.</value>
	public int Count => _list.Count;

	/// <summary>
	/// Gets a value that indicates whether the list is read-only.
	/// </summary>
	/// <value>Always <see langword="false"/>.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	bool ICollection<T>.IsReadOnly => false;

	/// <summary>
	/// Initializes a new, empty <see cref="MetadataList{T}"/> instance.
	/// </summary>
	public MetadataList()
	{
		_list = [];
	}
	/// <summary>
	/// Initializes a new, empty <see cref="MetadataList{T}"/> instance that can hold the specified number of elements
	/// before it grows.
	/// </summary>
	/// <param name="capacity">The number of elements that the list can hold before it grows.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is less than 0.</exception>
	public MetadataList(int capacity)
	{
		_list = new(capacity);
	}
	/// <summary>
	/// Initializes a new <see cref="MetadataList{T}"/> instance that holds the elements of the specified collection.
	/// </summary>
	/// <param name="items">The elements to copy into the list, or <see langword="null"/> for an empty list.</param>
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
	/// Initializes a new <see cref="MetadataList{T}"/> instance that holds copies of the specified values.
	/// </summary>
	/// <param name="values">The values to copy into the list. A later change to the span doesn't affect the list.</param>
	internal MetadataList(params ReadOnlySpan<T> values)
	{
		_list = [.. values];
	}

	/// <summary>
	/// Adds an element to the end of the list, unless it's null.
	/// </summary>
	/// <param name="item">The element to add. A <see langword="null"/> isn't added.</param>
	public void Add(T item)
	{
		if (item is not null)
		{
			_list.Add(item);
		}
	}

	/// <summary>
	/// Adds the elements of a collection to the end of the list.
	/// </summary>
	/// <param name="collection">The elements to add, or <see langword="null"/> to add none.</param>
	public void AddRange(IEnumerable<T>? collection)
	{
		if (collection is not null)
		{
			_list.AddRange(collection);
		}
	}

	/// <summary>
	/// Removes every element from the list.
	/// </summary>
	/// <remarks>
	/// The list keeps its capacity.
	/// </remarks>
	public void Clear()
	{
		_list.Clear();
	}
	/// <summary>
	/// Determines whether the list holds an element that equals the specified one or sorts with it.
	/// </summary>
	/// <param name="item">The element to look for.</param>
	/// <returns>
	/// <see langword="true"/> if an element equals <paramref name="item"/> or compares to it as 0; otherwise,
	/// <see langword="false"/>.
	/// </returns>
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
	/// Copies the list's elements to an array, starting at the specified index of the array.
	/// </summary>
	/// <param name="array">The array to copy the elements to.</param>
	/// <param name="index">The index in <paramref name="array"/> at which copying starts.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is null.</exception>
	/// <exception cref="ArgumentException">
	/// Thrown when the elements don't fit in <paramref name="array"/> from <paramref name="index"/> on.
	/// </exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is less than 0.</exception>
	public void CopyTo(T[] array, int index)
	{
		_list.CopyTo(array, index);
	}
	/// <summary>
	/// Finds the first element that matches a predicate.
	/// </summary>
	/// <param name="predicate">The predicate that an element must match.</param>
	/// <returns>The first matching element, or <see langword="default"/> when none matches.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
	public T? Find(Predicate<T> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		return _list.Find(predicate);
	}

	/// <summary>
	/// Gets the index of the first element that equals the specified one.
	/// </summary>
	/// <param name="item">The element to look for.</param>
	/// <returns>The zero-based index of the first equal element, or -1 when there's none.</returns>
	public int IndexOf(T item)
	{
		return _list.IndexOf(item);
	}
	/// <summary>
	/// Inserts an element at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index to insert <paramref name="item"/> at.</param>
	/// <param name="item">The element to insert.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="index"/> is less than 0 or greater than <see cref="Count"/>.
	/// </exception>
	public void Insert(int index, T item)
	{
		_list.Insert(index, item);
	}
	/// <summary>
	/// Removes the first element that equals the specified one.
	/// </summary>
	/// <param name="item">The element to remove.</param>
	/// <returns>
	/// <see langword="true"/> if an element was removed; otherwise, <see langword="false"/>.
	/// </returns>
	public bool Remove(T item)
	{
		return _list.Remove(item);
	}
	/// <summary>
	/// Removes the element at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the element to remove.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="index"/> is less than 0, or is equal to or greater than <see cref="Count"/>.
	/// </exception>
	public void RemoveAt(int index)
	{
		_list.RemoveAt(index);
	}
	/// <summary>
	/// Removes every element that matches a predicate, which receives two extra arguments.
	/// </summary>
	/// <typeparam name="TArg1">The type of the predicate's first extra argument.</typeparam>
	/// <typeparam name="TArg2">The type of the predicate's second extra argument.</typeparam>
	/// <param name="arg1">The first extra argument to pass to the predicate.</param>
	/// <param name="arg2">The second extra argument to pass to the predicate.</param>
	/// <param name="predicate">
	/// The function that receives each element, <paramref name="arg1"/>, and <paramref name="arg2"/>, and returns
	/// <see langword="true"/> to remove the element.
	/// </param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="predicate"/> holds a null function pointer.</exception>
	internal void RemoveAll<TArg1, TArg2>(TArg1 arg1, TArg2 arg2, FnPtr<T, TArg1, TArg2, bool> predicate)
		where TArg1 : allows ref struct
		where TArg2 : allows ref struct
	{
		FnPtr.ThrowIfNull(predicate);

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

	/// <summary>
	/// Tags each element of the list with its metadata tag.
	/// </summary>
	/// <param name="resolver">The resolver that the elements get their tags from.</param>
	public void SetTag(IMetadataResolver resolver)
	{
		foreach (T item in CollectionsMarshal.AsSpan(_list))
		{
			item.SetTag(resolver);
		}
	}

	/// <summary>
	/// Sorts the list's elements by <typeparamref name="T"/>'s own order.
	/// </summary>
	/// <remarks>
	/// The sort isn't stable, so elements that compare as equal can change places.
	/// </remarks>
	public void Sort()
	{
		_list.Sort();
	}

	/// <summary>
	/// Finds the first element that matches a predicate, which receives a state argument.
	/// </summary>
	/// <typeparam name="TState">The type of the predicate's state argument.</typeparam>
	/// <param name="state">The state to pass to the predicate with each element.</param>
	/// <param name="predicate">
	/// The function that receives each element and <paramref name="state"/>, and returns <see langword="true"/> for a
	/// match.
	/// </param>
	/// <param name="result">
	/// When this method returns <see langword="true"/>, the first matching element; otherwise,
	/// <see langword="default"/>.
	/// </param>
	/// <returns><see langword="true"/> if an element matched; otherwise, <see langword="false"/>.</returns>
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

