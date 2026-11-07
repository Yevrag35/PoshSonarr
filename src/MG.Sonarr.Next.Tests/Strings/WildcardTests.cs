using MG.Sonarr.Next.Strings;

namespace MG.Sonarr.Next.Tests.Strings;

/// <summary>
/// Tests how a <see cref="Wildcard"/> matches, which every cmdlet that filters by name relies on.
/// </summary>
public sealed class WildcardTests
{
	/// <summary>
	/// The units that patterns and inputs are built from: both wildcards, letters that differ only in case, inside and
	/// outside ASCII, the Kelvin sign, which <see cref="StringComparison.OrdinalIgnoreCase"/> doesn't fold to <c>k</c>,
	/// characters that a <see cref="WildcardPattern"/> treats specially but a <see cref="Wildcard"/> doesn't, white
	/// space, and the Deseret capital and small long I, a surrogate pair each.
	/// </summary>
	private static readonly Gen<string> s_text = Gen.OneOf(
			"*", "?", "a", "A", "b", "é", "É", "k", "K", "%", "[", "`", " ", "\U00010400", "\U00010428")
		.Array[0, 8]
		.Select(units => string.Concat(units));

	/// <summary>
	/// B36, B37, B42: under the default comparison, <see cref="StringComparison.OrdinalIgnoreCase"/>, a pattern matches as
	/// its docs say: a <c>*</c> takes any number of characters wherever it stands, also where the input holds a
	/// <c>*</c>, a <c>?</c> takes one UTF-16 code unit, every other character, <c>%</c> included, matches what
	/// <see cref="StringComparison.OrdinalIgnoreCase"/> finds equal to it, a surrogate pair as one character, and an
	/// empty pattern matches nothing.
	/// </summary>
	/// <remarks>
	/// The matcher is tuned for speed, so the test holds it to a plain recursive matcher of the same rules, first on the
	/// cases that found the bugs and then on random patterns and inputs.
	/// </remarks>
	[Fact]
	public void B36_MatchesAsAPlainMatcherOfItsRulesDoes()
	{
		(string Pattern, string Input, bool Expected)[] known =
		[
			("M*H", "M*A*S*H", true),
			("a*b", "a*xb", true),
			("*?", "*", true),
			("*", "*a", true),
			("*é*", "É", true),
			("é?", "É", false),
			("é?", "Éa", true),
			("*\U00010400*", "\U00010428", true),
			("\U00010400*", "\U00010428x", true),
			("?", "\U00010400", false),
			("??", "\U00010400", true),
			("%", "anything", false),
			("%", "%", true),
			("k", "K", false),
			("", "", false),
			("*", "", true),
		];

		Assert.Multiple(
		[
			.. known.Select(c => (Action)(() =>
			{
				Assert.True(c.Expected == Reference(c.Pattern, c.Input), $"The reference matcher gets '{c.Pattern}' against '{c.Input}' wrong.");
				Assert.True(c.Expected == Wildcard.Parse(c.Pattern).IsMatch(c.Input), $"'{c.Pattern}' against '{c.Input}' should give {c.Expected}.");
			})),
		]);

		Gen.Select(s_text, s_text).Sample(
			(pattern, input) => Wildcard.Parse(pattern).IsMatch(input) == Reference(pattern, input),
			iter: 20_000,
			print: t => $"pattern '{t.Item1}' against input '{t.Item2}'");
	}

	/// <summary>
	/// Matches a pattern against an input by the rules in <see cref="Wildcard"/>'s docs, trying every split at each
	/// <c>*</c>.
	/// </summary>
	private static bool Reference(string pattern, string input)
	{
		return pattern.Length > 0 && Matches(pattern, 0, input, 0);

		static bool Matches(string pattern, int p, string input, int i)
		{
			if (p == pattern.Length)
			{
				return i == input.Length;
			}

			char c = pattern[p];
			if (c == '*')
			{
				for (int next = i; next <= input.Length; next++)
				{
					if (Matches(pattern, p + 1, input, next))
					{
						return true;
					}
				}

				return false;
			}

			if (c == '?')
			{
				return i < input.Length && Matches(pattern, p + 1, input, i + 1);
			}

			int width = char.IsHighSurrogate(c) && p + 1 < pattern.Length && char.IsLowSurrogate(pattern[p + 1]) ? 2 : 1;

			return i + width <= input.Length
				&& pattern.AsSpan(p, width).Equals(input.AsSpan(i, width), StringComparison.OrdinalIgnoreCase)
				&& Matches(pattern, p + width, input, i + width);
		}
	}
}
