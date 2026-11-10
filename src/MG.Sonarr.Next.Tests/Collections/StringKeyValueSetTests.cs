using MG.Sonarr.Next.Collections;

namespace MG.Sonarr.Next.Tests.Collections;

/// <summary>
/// Tests the set of terms and scores that a release profile's preferred terms are read into.
/// </summary>
public sealed class StringKeyValueSetTests
{
	/// <summary>
	/// The keys that the changes use: terms that differ only in case, inside and outside ASCII, the dotless small i and
	/// the capital I, the sharp s and its uppercase spelling, and a term with a space.
	/// </summary>
	private static readonly Gen<string> s_keys = Gen.OneOf("x265", "X265", "HEVC", "hevc", "é", "É", "ı", "I", "ß", "SS", "a b");

	/// <summary>
	/// The changes that the property makes to a set.
	/// </summary>
	private static readonly Gen<SetChange> s_changes = Gen.OneOf<SetChange>(
		Gen.Select(s_keys, Gen.Int, (key, value) => (SetChange)new AddPair(key, value)),
		Gen.Select(s_keys, Gen.Int, (key, value) => (SetChange)new SetValue(key, value)),
		s_keys.Select(key => (SetChange)new RemoveKey(key)));

	/// <summary>
	/// A <see cref="StringKeyValueSet{T}"/> holds one pair for each key under <see cref="StringComparer.OrdinalIgnoreCase"/>,
	/// in key order, as a <see cref="SortedDictionary{TKey, TValue}"/> with that comparer does after the same changes.
	/// <c>Add</c> adds a pair only for a key that the set doesn't hold, the indexer's setter replaces the pair, so the key
	/// takes the case that it was set with, and <c>Remove</c> takes the pair out. <c>ContainsKey</c>,
	/// <c>TryGetValue</c>, and the indexer's getter find a key in any case, and the getter gives the default value for a
	/// key that the set doesn't hold.
	/// </summary>
	[Fact]
	public void HoldsThePairsThatASortedDictionaryHoldsAfterTheSameChanges()
	{
		Gen.Select(s_changes.Array[0, 30], s_keys).Sample((changes, probe) =>
		{
			StringKeyValueSet<int> set = new();
			SortedDictionary<string, int> model = new(StringComparer.OrdinalIgnoreCase);

			foreach (SetChange change in changes)
			{
				switch (change)
				{
					case AddPair(string key, int value):
						Assert.Equal(model.TryAdd(key, value), set.Add(key, value));
						break;

					case SetValue(string key, int value):
						set[key] = value;
						_ = model.Remove(key);
						model.Add(key, value);
						break;

					case RemoveKey(string key):
						Assert.Equal(model.Remove(key), set.Remove(key));
						break;

					default:
						throw new UnreachableException($"No case for {change}.");
				}

				Assert.Equal(model.Select(pair => (pair.Key, pair.Value)).ToArray(), set.Select(pair => (pair.Key, pair.Value)).ToArray());
				Assert.Equal(model.Count, set.Count);
				Assert.Equal(model.ContainsKey(probe), set.ContainsKey(probe));
				Assert.Equal(model.TryGetValue(probe, out int expected), set.TryGetValue(probe, out int actual));
				Assert.Equal(expected, actual);
				Assert.Equal(expected, set[probe]);
			}
		}, iter: 3_000, print: t => $"{string.Join(", ", t.Item1)}, probing for '{t.Item2}'");
	}

	/// <summary>
	/// A change that the property makes to a set.
	/// </summary>
	private abstract record SetChange;

	private sealed record AddPair(string Key, int Value) : SetChange;

	private sealed record SetValue(string Key, int Value) : SetChange;

	private sealed record RemoveKey(string Key) : SetChange;
}
