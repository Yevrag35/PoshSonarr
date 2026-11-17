using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Tests.Metadata;

/// <summary>
/// Tests the list that a request's models arrive in, which the client tags and sorts and the cmdlets write in order.
/// </summary>
public sealed class MetadataListTests
{
	/// <summary>
	/// The number of elements that each run of the property can use, which <see cref="s_items"/> picks among.
	/// </summary>
	private const int POOL_SIZE = 6;

	/// <summary>
	/// The elements that a change uses, as indexes into the run's pool, or -1 for <see langword="null"/>.
	/// </summary>
	private static readonly Gen<int> s_items = Gen.Int[-1, POOL_SIZE - 1];

	/// <summary>
	/// The changes that the property makes to a list.
	/// </summary>
	private static readonly Gen<ListChange> s_changes = Gen.OneOf<ListChange>(
		s_items.Select(item => (ListChange)new AddItem(item)),
		s_items.Array[0, 6].Select(items => (ListChange)new AddRangeOfEnumerable(items)),
		s_items.Array[0, 6].Select(items => (ListChange)new AddRangeOfSpan(items)),
		s_items.Array[0, 6].Select(items => (ListChange)new AddRangeOfList(items)),
		Gen.Const<ListChange>(new AddRangeOfItself()),
		Gen.Select(Gen.Int[0, 100], s_items, (index, item) => (ListChange)new InsertItem(index, item)),
		Gen.Select(Gen.Int[0, 100], s_items, (index, item) => (ListChange)new SetItem(index, item)),
		s_items.Select(item => (ListChange)new RemoveItem(item)),
		Gen.Int[0, 100].Select(index => (ListChange)new RemoveItemAt(index)),
		Gen.Const<ListChange>(new ClearAll()),
		Gen.Const<ListChange>(new SortAll()),
		Gen.Const<ListChange>(new TagAll()));

	/// <summary>
	/// A <see cref="MetadataList{T}"/> holds the elements that a <see cref="List{T}"/> holds after the same changes,
	/// except that <c>Add</c>, <c>AddRange</c> of a span, a collection expression, and the constructor that copies an
	/// <see cref="IEnumerable{T}"/> leave out <see langword="null"/>, as their docs say, while <c>AddRange</c> of an
	/// <see cref="IEnumerable{T}"/> or of another list, which can be the list itself, <c>Insert</c>, and the indexer put
	/// in what they're given. Every way of enumerating it, its span, and <c>CopyTo</c> give its elements in order,
	/// <c>Sort</c> orders them as <see cref="List{T}.Sort()"/> does, and <c>SetTag</c> tags each element once for each
	/// place that it holds and passes over <see langword="null"/>. <c>Contains</c> and <c>IndexOf</c> find an element
	/// as the <see cref="List{T}"/> does, except that the list contains no <see langword="null"/>.
	/// </summary>
	[Fact]
	public void HoldsTheElementsThatAListHoldsAfterTheSameChanges()
	{
		Item[] fixedPool = NewPool();
		Item[] withNull = [fixedPool[0], null!, fixedPool[1]];
		MetadataList<Item> fromExpression = [fixedPool[0], null!, fixedPool[1]];
		Assert.Equal([fixedPool[0], fixedPool[1]], fromExpression.AsSpan().ToArray());
		Assert.Equal([fixedPool[0], fixedPool[1]], new MetadataList<Item>((IEnumerable<Item>)withNull).AsSpan().ToArray());

		Gen.Select(s_changes.Array[0, 30], Gen.Int[0, 5]).Sample((changes, probeKey) =>
		{
			Item[] pool = NewPool();
			MetadataList<Item> list = new();
			List<Item> model = [];
			Dictionary<Item, int> timesTagged = pool.ToDictionary(item => item, _ => 0);

			foreach (ListChange change in changes)
			{
				Apply(list, model, pool, timesTagged, change);
				AssertHolds(list, model, pool, timesTagged, probeKey);
			}
		}, iter: 3_000, print: t => $"{string.Join(", ", t.Item1)}, probing for key {t.Item2}");
	}

	/// <summary>
	/// Makes a change to a list and to the <see cref="List{T}"/> that models it.
	/// </summary>
	private static void Apply(MetadataList<Item> list, List<Item> model, Item[] pool, Dictionary<Item, int> timesTagged, ListChange change)
	{
		int count = model.Count;
		switch (change)
		{
			case AddItem(int item):
				list.Add(Pick(pool, item));
				if (item >= 0)
				{
					model.Add(pool[item]);
				}

				break;

			case AddRangeOfEnumerable(int[] items):
				list.AddRange((IEnumerable<Item>)Pick(pool, items));
				model.AddRange(Pick(pool, items));
				break;

			case AddRangeOfSpan(int[] items):
				list.AddRange((ReadOnlySpan<Item>)Pick(pool, items));
				model.AddRange(Pick(pool, items).Where(item => item is not null));
				break;

			case AddRangeOfList(int[] items):
				MetadataList<Item> other = new();
				foreach (Item item in Pick(pool, items))
				{
					other.Insert(other.Count, item);
				}

				list.AddRange(other);
				model.AddRange(Pick(pool, items));
				break;

			case AddRangeOfItself:
				list.AddRange(list);
				model.AddRange([.. model]);
				break;

			case InsertItem(int index, int item):
				list.Insert(index % (count + 1), Pick(pool, item));
				model.Insert(index % (count + 1), Pick(pool, item));
				break;

			case SetItem(int index, int item) when count > 0:
				list[index % count] = Pick(pool, item);
				model[index % count] = Pick(pool, item);
				break;

			case RemoveItem(int item):
				Assert.Equal(model.Remove(Pick(pool, item)), list.Remove(Pick(pool, item)));
				break;

			case RemoveItemAt(int index) when count > 0:
				list.RemoveAt(index % count);
				model.RemoveAt(index % count);
				break;

			case SetItem or RemoveItemAt:
				break;

			case ClearAll:
				list.Clear();
				model.Clear();
				break;

			case SortAll:
				list.Sort();
				model.Sort();
				break;

			case TagAll:
				list.SetTag(ModuleServices.Resolver);
				foreach (Item item in model)
				{
					if (item is not null)
					{
						timesTagged[item]++;
					}
				}

				break;

			default:
				throw new UnreachableException($"No case for {change}.");
		}
	}

	/// <summary>
	/// Asserts that a list holds its model's elements, in order, whichever way it's read.
	/// </summary>
	private static void AssertHolds(MetadataList<Item> list, List<Item> model, Item[] pool, Dictionary<Item, int> timesTagged, int probeKey)
	{
		Item[] expected = [.. model];

		List<Item> enumerated = [];
		foreach (Item item in list)
		{
			enumerated.Add(item);
		}

		Item[] copied = new Item[expected.Length + 1];
		list.CopyTo(copied, 1);

		Assert.Equal(expected.Length, list.Count);
		Assert.Equal(expected, enumerated);
		Assert.Equal(expected, ((IEnumerable<Item>)list).ToArray());
		Assert.Equal(expected, ((IEnumerable)list).Cast<Item>().ToArray());
		Assert.Equal(expected, list.AsSpan().ToArray());
		Assert.Equal(expected, copied[1..]);

		for (int i = 0; i < expected.Length; i++)
		{
			Assert.Same(expected[i], list[i]);
		}

		foreach (Item? item in (Item?[])[.. pool, null])
		{
			Assert.Equal(item is not null && model.Contains(item), list.Contains(item!));
			Assert.Equal(model.IndexOf(item!), list.IndexOf(item!));
		}

		Item? first = model.Find(item => item is not null && item.Key == probeKey);
		Assert.Equal(first is not null, list.TryFind(probeKey, static (item, key) => item is not null && item.Key == key, out Item? found));
		Assert.Same(first, found);

		foreach (Item item in pool)
		{
			Assert.Equal(timesTagged[item], item.TimesTagged);
		}
	}

	/// <summary>
	/// Creates the elements that a run of the property uses: six, two of which share a key with another, so that a sort
	/// meets elements that compare as equal.
	/// </summary>
	private static Item[] NewPool()
	{
		return [new(0, 0), new(1, 1), new(2, 1), new(3, 2), new(4, 3), new(5, 3)];
	}

	/// <summary>
	/// Gives the element of a pool at the specified index, or <see langword="null"/> for -1.
	/// </summary>
	private static Item Pick(Item[] pool, int index)
	{
		return index < 0 ? null! : pool[index];
	}

	/// <summary>
	/// Gives the elements of a pool at the specified indexes, with <see langword="null"/> for -1.
	/// </summary>
	private static Item[] Pick(Item[] pool, int[] indexes)
	{
		return [.. indexes.Select(index => Pick(pool, index))];
	}

	/// <summary>
	/// An element of a list, which sorts by its key, equals only itself, and counts the times that it's tagged.
	/// </summary>
	private sealed class Item : IComparable<Item>, IJsonMetadataTaggable
	{
		/// <summary>
		/// Gets the element's place in its pool, which names it.
		/// </summary>
		/// <value>The element's index in its pool.</value>
		internal int Name { get; }

		/// <summary>
		/// Gets the key that the element sorts by.
		/// </summary>
		/// <value>The element's key, which other elements can share.</value>
		internal int Key { get; }

		/// <summary>
		/// Gets the number of times that the element has been tagged.
		/// </summary>
		/// <value>The number of calls to <see cref="SetTag(IMetadataResolver)"/>.</value>
		internal int TimesTagged { get; private set; }

		internal Item(int name, int key)
		{
			this.Name = name;
			this.Key = key;
		}

		public int CompareTo(Item? other)
		{
			return other is null ? 1 : this.Key.CompareTo(other.Key);
		}

		public void SetTag(IMetadataResolver resolver)
		{
			this.TimesTagged++;
		}

		public override string ToString()
		{
			return string.Create(CultureInfo.InvariantCulture, $"#{this.Name} (key {this.Key})");
		}
	}

	/// <summary>
	/// A change that the property makes to a list, which names its elements by their index in the run's pool, or -1 for
	/// <see langword="null"/>.
	/// </summary>
	private abstract record ListChange;

	private sealed record AddItem(int Item) : ListChange;

	private sealed record AddRangeOfEnumerable(int[] Items) : ListChange
	{
		public override string ToString()
		{
			return $"AddRangeOfEnumerable [{string.Join(", ", this.Items)}]";
		}
	}

	private sealed record AddRangeOfSpan(int[] Items) : ListChange
	{
		public override string ToString()
		{
			return $"AddRangeOfSpan [{string.Join(", ", this.Items)}]";
		}
	}

	private sealed record AddRangeOfList(int[] Items) : ListChange
	{
		public override string ToString()
		{
			return $"AddRangeOfList [{string.Join(", ", this.Items)}]";
		}
	}

	private sealed record AddRangeOfItself : ListChange;

	private sealed record InsertItem(int Index, int Item) : ListChange;

	private sealed record SetItem(int Index, int Item) : ListChange;

	private sealed record RemoveItem(int Item) : ListChange;

	private sealed record RemoveItemAt(int Index) : ListChange;

	private sealed record ClearAll : ListChange;

	private sealed record SortAll : ListChange;

	private sealed record TagAll : ListChange;
}
