using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Strings;

namespace MG.Sonarr.Next.Tests.Collections;

/// <summary>
/// Tests the set of names that the cmdlets filter their results by.
/// </summary>
public sealed class WildcardSetTests
{
	/// <summary>
	/// The units that names are built from, which an empty draw leaves empty: digits, as an ID's text, letters that
	/// differ only in case, inside and outside ASCII and in a surrogate pair, both wildcards, and white space.
	/// </summary>
	private static readonly Gen<string> s_names = Gen.OneOf("5", "a", "B", "é", "É", "\U00010400", "*", "?", " ")
		.Array[0, 5]
		.Select(units => string.Concat(units));

	/// <summary>
	/// The ways that a cmdlet's names reach a set.
	/// </summary>
	private static readonly (string Name, Func<WildcardSet, string, bool> Add)[] s_adds =
	[
		("Add(string)", (set, name) => set.Add(name)),
		("Add(ReadOnlySpan<char>)", (set, name) => set.Add(name.AsSpan())),
		("Add(Wildcard)", (set, name) => set.Add(Wildcard.Parse(name))),
		("UnionWith(IEnumerable<string>)", (set, name) =>
		{
			int count = set.Count;
			set.UnionWith([name]);
			return set.Count > count;
		}),
	];

	/// <summary>
	/// B49, O60, O62: every member of a <see cref="WildcardSet"/> that adds text adds the pattern that
	/// <see cref="Wildcard.Parse(string)"/> makes of it, empty text and white space included, and two patterns are one
	/// item when their text is equal under <see cref="StringComparison.OrdinalIgnoreCase"/>, whichever member adds or
	/// looks them up. So an empty name matches nothing, as every cmdlet that filters by name needs, and a name that one
	/// member adds is found and kept once by every other.
	/// </summary>
	[Fact]
	public void B49_EveryMemberThatAddsTextAddsTheSamePattern()
	{
		WildcardSet ids = [];
		Assert.True(ids.Add(5));
		Assert.False(ids.Add("5"));
		Assert.False(ids.Add("5".AsSpan()));
		Assert.Single(ids);

		Assert.Throws<ArgumentNullException>(() => new WildcardSet().UnionWith((IEnumerable<string>)null!));

		s_names.Sample(name =>
		{
			Wildcard pattern = Wildcard.Parse(name);
			string[] probes = [name, name.ToUpperInvariant(), name.ToLowerInvariant(), "", " ", "x", name + "x"];

			foreach ((string addedBy, Func<WildcardSet, string, bool> add) in s_adds)
			{
				WildcardSet set = [];
				Assert.True(add(set, name), $"{addedBy} didn't add '{name}' to an empty set.");

				foreach ((string addedAgainBy, Func<WildcardSet, string, bool> addAgain) in s_adds)
				{
					Assert.False(addAgain(set, name.ToUpperInvariant()), $"{addedAgainBy} added '{name.ToUpperInvariant()}' beside {addedBy}'s '{name}'.");
				}

				Assert.Single(set);
				Assert.True(set.Contains(name.AsSpan()), $"Contains(ReadOnlySpan<char>) missed what {addedBy} added for '{name}'.");
				Assert.True(set.Contains(pattern), $"Contains(Wildcard) missed what {addedBy} added for '{name}'.");

				foreach (string probe in probes)
				{
					Assert.True(
						pattern.IsMatch(probe) == set.IsAnyMatch(probe),
						$"After {addedBy} of '{name}', IsAnyMatch('{probe}') disagreed with the pattern's IsMatch.");
				}

				if (name.Length == 0)
				{
					Assert.False(set.IsAnyMatch(""), $"An empty name that {addedBy} added matched empty text.");
				}
			}
		}, iter: 3_000);
	}
}
