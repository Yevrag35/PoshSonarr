using MG.Sonarr.Next.Extensions;
using System.Runtime.CompilerServices;

namespace MG.Sonarr.Next.Shell.Components;

/// <summary>
/// Represents a lightweight wildcard pattern matcher for character spans.
/// </summary>
/// <remarks>
/// <para>
/// In the pattern, <c>*</c> matches any run of characters, an empty one included, and <c>?</c> matches exactly one
/// character. Every other character matches itself, ignoring case as <see cref="char.ToLowerInvariant(char)"/> folds
/// it. There's no escape character, so a <c>*</c> or <c>?</c> in a pattern is always a wildcard.
/// </para>
/// <para>
/// The matcher refers to the span that it was given rather than copying it, so it lives no longer than that span, and a
/// change to the span's characters changes the pattern. A <see langword="default"/> instance has an empty pattern,
/// which matches nothing.
/// </para>
/// <para><b>Performance:</b> Matching allocates nothing. It remembers only the last <c>*</c> it passed, and on a
/// mismatch it goes back to that <c>*</c> and lets it take one more input character, so a pattern with stars can read
/// parts of the input more than once. It reads characters without bounds checks, behind its own index checks.</para>
/// </remarks>
public readonly ref struct PatternMatcher
{
	private const char QUESTION = '?';
	private const char STAR = '*';

	private readonly ReadOnlySpan<char> _pattern;

	/// <summary>
	/// Gets a value that indicates whether the pattern is empty.
	/// </summary>
	/// <value><see langword="true"/> when the pattern length is zero; otherwise, <see langword="false"/>.</value>
	public bool IsEmpty => _pattern.IsEmpty;

	/// <summary>
	/// Gets the pattern length.
	/// </summary>
	/// <value>The number of characters in the pattern.</value>
	public int Length => _pattern.Length;

	/// <summary>
	/// Initializes a new <see cref="PatternMatcher"/> instance with the specified pattern.
	/// </summary>
	/// <param name="span">The wildcard pattern to evaluate.</param>
	public PatternMatcher(ReadOnlySpan<char> span)
	{
		_pattern = span;
	}

	/// <summary>
	/// Determines whether two characters are equal using case-insensitive comparison.
	/// </summary>
	/// <remarks>
	/// Two characters are equal when <see cref="char.ToLowerInvariant(char)"/> gives the same character for both. A pair
	/// of ASCII characters compares inline, and any other pair goes to a helper that isn't inlined, because
	/// <see cref="char.ToLowerInvariant(char)"/> folds some characters outside ASCII to ASCII ones, as it does the
	/// Kelvin sign, U+212A, to <c>k</c>.
	/// </remarks>
	/// <param name="x">The first character.</param>
	/// <param name="y">The second character.</param>
	/// <returns><see langword="true"/> when the characters are equal ignoring case; otherwise, <see langword="false"/>.</returns>
	private static bool AreCharactersEqual(char x, char y)
	{
		if (x == y)
		{
			return true;
		}

		if ((x | y) <= 0x7F)
		{
			// ToLowerInvariant changes only A to Z in ASCII, so two different ASCII characters are equal only as one
			// letter in two cases. The range check turns away the other pairs that differ only in 0x20, such as @ and `.
			uint lower = (uint)(x | 0x20);
			return lower == (uint)(y | 0x20) && lower - 'a' <= 'z' - 'a';
		}

		return AreNonAsciiEqual(x, y);
	}

	/// <summary>
	/// Determines whether two characters, at least one of them outside ASCII, are equal using case-insensitive
	/// comparison.
	/// </summary>
	/// <param name="x">The first character.</param>
	/// <param name="y">The second character.</param>
	/// <returns>
	/// <see langword="true"/> when <see cref="char.ToLowerInvariant(char)"/> gives the same character for both;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool AreNonAsciiEqual(char x, char y)
	{
		return char.ToLowerInvariant(x) == char.ToLowerInvariant(y);
	}

	/// <summary>
	/// Determines whether the specified input matches the stored wildcard pattern.
	/// </summary>
	/// <remarks>
	/// The whole input has to match, not a part of it. An empty pattern matches no input, an empty one included, and an
	/// empty input matches only a pattern made of nothing but <c>*</c> characters.
	/// </remarks>
	/// <param name="input">The input characters to test.</param>
	/// <returns><see langword="true"/> when <paramref name="input"/> matches the pattern; otherwise, <see langword="false"/>.</returns>
	public bool IsMatch(ReadOnlySpan<char> input)
	{
		if (this.IsEmpty) return false;
		if (input.IsEmpty) return !_pattern.ContainsAnyExcept(STAR);

		int starIndex = -1;
		int iIndex = -1;

		int i = 0;
		int j = 0;

		ref char inputRef = ref Unsafe.AsRef(in input.Ref());
		ref char patRef = ref Unsafe.AsRef(in _pattern.Ref());

		while (i < input.Length)
		{
			if (j < _pattern.Length && patRef.Offset(j) == STAR)
			{
				// A star that ends the pattern matches the rest of the input.
				if (j == _pattern.Length - 1)
				{
					return true;
				}

				starIndex = j;
				iIndex = i;
				++j;
			}
			else if (j < _pattern.Length && (patRef.Offset(j) == QUESTION || AreCharactersEqual(patRef.Offset(j), inputRef.Offset(i))))
			{
				++i;
				++j;
			}
			else if (starIndex == -1)
			{
				return false;
			}
			else
			{
				j = starIndex + 1;
				i = iIndex + 1;
				iIndex++;
			}
		}

		while (j < _pattern.Length && patRef.Offset(j) == STAR)
		{
			++j;
		}

		return j == _pattern.Length;
	}
}
