using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Models.Episodes;

namespace MG.Sonarr.Next.Shell.Components;

/// <summary>
/// Represents the episodes that a value of <c>Get-SonarrEpisode</c>'s <c>-EpisodeIdentifier</c> names: a season and a
/// range of episode numbers in it, or a range of absolute episode numbers.
/// </summary>
/// <remarks>
/// <para>
/// Text such as <c>S01E02</c> names an episode of a season, <c>S01E02-05</c> or <c>S01E02-E05</c> a range of them,
/// and <c>S01</c> the whole season. Season 0 holds the specials, so <c>S00</c> names all of them, and episode 0 is an
/// episode like any other, so <c>S01E00</c> names episode 0 of season 1. <c>E25</c> or a whole number such as
/// <c>25</c> names an episode by its absolute number, and <c>E25-28</c> or <c>E25-E28</c> a range of them. The letters
/// ignore case. Text that strays from these forms in any part, such as <c>S01Ex</c> or <c>S01E02E05</c>, doesn't
/// parse.
/// </para>
/// <para>
/// PowerShell converts a string to the identifier through the <see cref="string"/> operator, and an <see cref="int"/>,
/// however it's written, such as <c>0x10</c> or <c>1kb</c>, through the <see cref="int"/> one, as an absolute episode
/// number. Any other number goes through the <see cref="string"/> operator as its text, and such a number typed as a
/// command argument keeps the text it was typed as, so <c>1e5</c> doesn't parse, while <c>(1e5)</c> names episode
/// 100000. An empty hashtable, an empty custom object, and a <c>$null</c> in a list given to
/// <c>Get-SonarrEpisode</c> convert to the <see langword="default"/> identifier, and other values, such as
/// <c>$true</c> or a custom object with properties, don't convert.
/// </para>
/// <para>
/// An identifier keeps the text that it was converted from, which <see cref="ToString"/> returns. Text that doesn't
/// parse and a negative <see cref="int"/> give an identifier that names no episodes and isn't valid, as the
/// <see langword="default"/> identifier is, so <c>Get-SonarrEpisode</c> stops with an error that names its text, as it
/// does for an identifier that parses but isn't valid, such as <c>S01E05-03</c>.
/// </para>
/// </remarks>
[DebuggerDisplay(@"{GetDebugString()}")]
public readonly struct SeasonEpisodeId : IEpisodeIdentifier
{
	readonly int _season;
	readonly bool _isAbsolute;
	readonly EpisodeRange _range;
	readonly string? _text;

	/// <summary>
	/// Gets the range of episode numbers.
	/// </summary>
	/// <value>
	/// The range of absolute episode numbers when <see cref="IsAbsolute"/> is <see langword="true"/>, or else the range
	/// of episode numbers in <see cref="Season"/>: <see cref="MG.Sonarr.Next.Models.Episodes.EpisodeRange.All"/> for a
	/// whole season, such as <c>S01</c>, and the <see langword="default"/> range, which matches no episodes, when
	/// <see cref="IsEmpty"/> is <see langword="true"/>.
	/// </value>
	public EpisodeRange EpisodeRange => _range;
	/// <summary>
	/// Gets a value that indicates whether <see cref="EpisodeRange"/> holds absolute episode numbers.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if the range holds absolute episode numbers; otherwise, <see langword="false"/>.
	/// </value>
	public bool IsAbsolute => _isAbsolute;
	/// <summary>
	/// Gets a value that indicates whether the identifier names no episodes.
	/// </summary>
	/// <value>
	/// <see langword="true"/> for the <see langword="default"/> identifier, and for one converted from text that
	/// doesn't parse or from a negative <see cref="int"/>; otherwise, <see langword="false"/>, as for a whole season,
	/// such as <c>S01</c>, or episode 0, such as <c>S01E00</c>.
	/// </value>
	public bool IsEmpty => !_range.IsSet;
	/// <summary>
	/// Gets the season number.
	/// </summary>
	/// <value>
	/// The number of the season that <see cref="EpisodeRange"/> is in, or 0 when <see cref="IsAbsolute"/> or
	/// <see cref="IsEmpty"/> is <see langword="true"/>, since absolute episode numbers aren't in a season.
	/// </value>
	public int Season => _season;

	/// <summary>
	/// Initializes a new <see cref="SeasonEpisodeId"/> instance for every episode of a season.
	/// </summary>
	/// <param name="season">The season number.</param>
	/// <param name="text">The text that the identifier was parsed from, or <see langword="null"/> for none.</param>
	private SeasonEpisodeId(int season, string? text)
	{
		_season = season;
		_isAbsolute = false;
		_range = EpisodeRange.All;
		_text = text;
	}
	/// <summary>
	/// Initializes a new <see cref="SeasonEpisodeId"/> instance for a range of episode numbers, in a season or absolute.
	/// </summary>
	/// <param name="season">The season number, or 0 when <paramref name="isAbsolute"/> is <see langword="true"/>.</param>
	/// <param name="range">The range of episode numbers.</param>
	/// <param name="isAbsolute">
	/// <see langword="true"/> if <paramref name="range"/> holds absolute episode numbers; otherwise,
	/// <see langword="false"/>.
	/// </param>
	/// <param name="text">The text that the identifier was parsed from, or <see langword="null"/> for none.</param>
	private SeasonEpisodeId(int season, EpisodeRange range, bool isAbsolute, string? text)
	{
		_season = season;
		_isAbsolute = isAbsolute;
		_range = range;
		_text = text;
	}
	/// <summary>
	/// Initializes a new <see cref="SeasonEpisodeId"/> instance that names no episodes and keeps the text that it was
	/// converted from.
	/// </summary>
	/// <param name="text">The text, or <see langword="null"/> for none.</param>
	private SeasonEpisodeId(string? text)
	{
		_season = 0;
		_isAbsolute = false;
		_range = default;
		_text = text;
	}

	/// <summary>
	/// Determines whether the identifier names episodes and its range doesn't end before it starts.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if <see cref="IsEmpty"/> is <see langword="false"/> and <see cref="EpisodeRange"/> is
	/// valid; otherwise, <see langword="false"/>.
	/// </returns>
	bool IEpisodeIdentifier.IsValid()
	{
		return _range.IsValid();
	}

	/// <summary>
	/// Converts the text of an episode identifier to a <see cref="SeasonEpisodeId"/>, and returns a value that
	/// indicates whether it succeeded.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The text is <c>S</c> and a season number, for the whole season, or <c>S</c>, a season number, <c>E</c>, and an
	/// episode number, for an episode of the season. Without the season, <c>E</c> and an episode number, or a number
	/// alone, is an absolute episode number. After an <c>E</c>, a <c>-</c> and a second number, which can start with
	/// an <c>E</c> of its own, as in <c>S01E02-E05</c>, make a range that ends with that episode.
	/// </para>
	/// <para>
	/// The letters ignore case, each number is one or more of the digits 0 to 9, read in the invariant culture, and
	/// white space is ignored only before and after the text. Text that strays from this form in any part doesn't
	/// parse, as with a number before the <c>S</c> or the <c>E</c>, as in <c>10E05</c>, a sign, white space inside the
	/// text, a number too large for an <see cref="int"/>, an <c>E</c> that doesn't start a number of the range, as in
	/// <c>S01E02E03</c>, a season in the range's second number, as in <c>S01E02-S01E03</c>, or anything after the last
	/// number.
	/// </para>
	/// <para>
	/// The identifier doesn't keep <paramref name="value"/>, so its <see cref="ToString"/> returns the episodes that it
	/// names, in a form such as <c>S1E2</c>.
	/// </para>
	/// </remarks>
	/// <param name="value">The text, such as <c>S01E02</c>, <c>S01E02-05</c>, <c>S01</c>, <c>E25</c>, or <c>25</c>.</param>
	/// <param name="result">
	/// When the method returns, the identifier that <paramref name="value"/> names, or the <see langword="default"/>
	/// identifier when it doesn't parse.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="value"/> parses; otherwise, <see langword="false"/>, as when it's empty
	/// or white space. An identifier that parses still isn't valid when its range ends before it starts, as in
	/// <c>S01E05-03</c>.
	/// </returns>
	public static bool TryParse([ValidatedNotNull] ReadOnlySpan<char> value, out SeasonEpisodeId result)
	{
		return TryParseCore(value, text: null, out result);
	}
	/// <summary>
	/// Converts the text of an episode identifier to a <see cref="SeasonEpisodeId"/> that keeps the specified text, and
	/// returns a value that indicates whether it succeeded.
	/// </summary>
	/// <remarks>
	/// The text parses as <see cref="TryParse(ReadOnlySpan{char}, out SeasonEpisodeId)"/> describes.
	/// </remarks>
	/// <param name="value">The text to parse.</param>
	/// <param name="text">The text for the identifier to keep, or <see langword="null"/> for none.</param>
	/// <param name="result">
	/// When the method returns, the identifier that <paramref name="value"/> names, or the <see langword="default"/>
	/// identifier when it doesn't parse.
	/// </param>
	/// <returns><see langword="true"/> if <paramref name="value"/> parses; otherwise, <see langword="false"/>.</returns>
	private static bool TryParseCore(ReadOnlySpan<char> value, string? text, out SeasonEpisodeId result)
	{
		result = default;
		value = value.Trim();
		if (value.IsEmpty)
		{
			return false;
		}

		if (value[0] is 'S' or 's')
		{
			value = value.Slice(1);
			int episodeIndex = value.IndexOfAny('E', 'e');
			if (!TryParseNumber(episodeIndex < 0 ? value : value.Slice(0, episodeIndex), out int season))
			{
				return false;
			}

			if (episodeIndex < 0)
			{
				result = new(season, text);
				return true;
			}

			return TryParseRange(value.Slice(episodeIndex + 1), season, isAbsolute: false, text, out result);
		}

		if (value[0] is 'E' or 'e')
		{
			return TryParseRange(value.Slice(1), season: 0, isAbsolute: true, text, out result);
		}

		if (TryParseNumber(value, out int absolute))
		{
			result = new(0, new EpisodeRange(absolute, absolute), isAbsolute: true, text);
			return true;
		}

		return false;
	}
	/// <summary>
	/// Converts the text after an <c>E</c>, an episode number or a range of them, to a <see cref="SeasonEpisodeId"/>,
	/// and returns a value that indicates whether it succeeded.
	/// </summary>
	/// <param name="numbers">
	/// The text after the <c>E</c>: one number, such as <c>02</c>, or two joined by a <c>-</c>, where the second can
	/// start with an <c>E</c> of its own, such as <c>02-05</c> or <c>02-E05</c>.
	/// </param>
	/// <param name="season">The season number, or 0 when <paramref name="isAbsolute"/> is <see langword="true"/>.</param>
	/// <param name="isAbsolute">
	/// <see langword="true"/> if <paramref name="numbers"/> holds absolute episode numbers; otherwise,
	/// <see langword="false"/>.
	/// </param>
	/// <param name="text">The text for the identifier to keep, or <see langword="null"/> for none.</param>
	/// <param name="result">
	/// When the method returns, the identifier for the range, or the <see langword="default"/> identifier when
	/// <paramref name="numbers"/> doesn't parse.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="numbers"/> is one number, or two joined by a <c>-</c>; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	private static bool TryParseRange(ReadOnlySpan<char> numbers, int season, bool isAbsolute, string? text, out SeasonEpisodeId result)
	{
		result = default;
		int dashIndex = numbers.IndexOf('-');
		if (!TryParseNumber(dashIndex < 0 ? numbers : numbers.Slice(0, dashIndex), out int start))
		{
			return false;
		}

		int end = start;
		if (dashIndex >= 0)
		{
			ReadOnlySpan<char> last = numbers.Slice(dashIndex + 1);
			if (last is ['E' or 'e', ..])
			{
				last = last.Slice(1);
			}

			if (!TryParseNumber(last, out end))
			{
				return false;
			}
		}

		result = new(season, new EpisodeRange(start, end), isAbsolute, text);
		return true;
	}
	/// <summary>
	/// Converts text made of the digits 0 to 9 to a number, and returns a value that indicates whether it succeeded.
	/// </summary>
	/// <param name="text">The text, which has no sign, white space, or separator.</param>
	/// <param name="number">When the method returns, the number, or 0 when <paramref name="text"/> doesn't parse.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="text"/> is one or more digits whose number fits an <see cref="int"/>;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private static bool TryParseNumber(ReadOnlySpan<char> text, out int number)
	{
		return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number);
	}

	/// <summary>
	/// Returns the text of the identifier.
	/// </summary>
	/// <returns>
	/// The text that the identifier was converted from, as it was given, whether it parses or not. For an identifier
	/// converted from an <see cref="int"/> that isn't negative, or returned by
	/// <see cref="TryParse(ReadOnlySpan{char}, out SeasonEpisodeId)"/>, the episodes that it names, in the form
	/// <c>S1</c> for a whole season, <c>S1E2</c> or <c>S1E2-5</c> for episodes of a season, or <c>25</c> or
	/// <c>E25-28</c> for absolute episode numbers. An empty string for the <see langword="default"/> identifier.
	/// </returns>
	public override string ToString()
	{
		return _text ?? (_range.IsSet ? this.FormatEpisodes() : string.Empty);
	}

	/// <summary>
	/// Gets the text that the debugger shows for the identifier.
	/// </summary>
	/// <returns>
	/// The episodes that the identifier names, in the form that <see cref="FormatEpisodes"/> returns, or, for an
	/// identifier that names no episodes, its text in quotes, followed by <c>(empty)</c>.
	/// </returns>
	private string GetDebugString()
	{
		return _range.IsSet
			? this.FormatEpisodes()
			: string.Concat("\"", _text, "\" (empty)");
	}
	/// <summary>
	/// Formats the season and the episode numbers that the identifier names.
	/// </summary>
	/// <remarks>
	/// The text is built on the stack. Call it only for an identifier whose <see cref="EpisodeRange"/> was made from
	/// episode numbers, which <see cref="IsEmpty"/> reports.
	/// </remarks>
	/// <returns>
	/// The identifier in the form <c>S1</c> for a whole season, <c>S1E2</c> or <c>S1E2-5</c> for episodes of a season,
	/// or <c>25</c> or <c>E25-28</c> for absolute episode numbers.
	/// </returns>
	private string FormatEpisodes()
	{
		const int max_length = 3 + (LengthConstants.INT_MAX * 3);

		using SpanStringBuilder builder = new(stackalloc char[max_length]);
		if (!_isAbsolute)
		{
			builder.Append('S');
			builder.Append(_season, LengthConstants.INT_MAX, provider: CultureInfo.InvariantCulture);
			if (_range.AllMatch)
			{
				return builder.ToString();
			}

			builder.Append('E');
		}
		else if (!_range.IsSingle)
		{
			builder.Append('E');
		}

		builder.Append(_range.Start, LengthConstants.INT_MAX, provider: CultureInfo.InvariantCulture);
		if (!_range.IsSingle)
		{
			builder.Append('-');
			builder.Append(_range.End, LengthConstants.INT_MAX, provider: CultureInfo.InvariantCulture);
		}

		return builder.ToString();
	}

	/// <summary>
	/// Converts the text of an episode identifier to a <see cref="SeasonEpisodeId"/>.
	/// </summary>
	/// <param name="value">The text, such as <c>S01E02</c>, or <see langword="null"/>.</param>
	/// <returns>
	/// The identifier that <paramref name="value"/> names, which keeps the text, or, when <paramref name="value"/> is
	/// <see langword="null"/> or doesn't parse, an identifier that keeps the text but names no episodes and isn't
	/// valid.
	/// </returns>
	public static implicit operator SeasonEpisodeId(string? value)
	{
		return TryParseCore(value, value, out SeasonEpisodeId id) ? id : new SeasonEpisodeId(value);
	}
	/// <summary>
	/// Converts an absolute episode number to a <see cref="SeasonEpisodeId"/>.
	/// </summary>
	/// <param name="absoluteEpNumber">The episode's absolute number.</param>
	/// <returns>
	/// An absolute identifier for the one episode, or, when <paramref name="absoluteEpNumber"/> is negative, an
	/// identifier that names no episodes and isn't valid, whose text is the number.
	/// </returns>
	public static implicit operator SeasonEpisodeId(int absoluteEpNumber)
	{
		return absoluteEpNumber < 0
			? new SeasonEpisodeId(absoluteEpNumber.ToString(CultureInfo.InvariantCulture))
			: new SeasonEpisodeId(0, new EpisodeRange(absoluteEpNumber, absoluteEpNumber), isAbsolute: true, text: null);
	}
}
