using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Models.Episodes;

namespace MG.Sonarr.Next.Shell.Components;

/// <summary>
/// Represents the episodes that a value of <c>Get-SonarrEpisode</c>'s <c>-EpisodeIdentifier</c> names: a season and a
/// range of episode numbers in it, or a range of absolute episode numbers.
/// </summary>
/// <remarks>
/// <para>
/// Text such as <c>S01E02</c> names an episode of a season, <c>S01E02-05</c> a range of them, and <c>S01</c> the whole
/// season. Season 0 holds the specials, so <c>S00</c> names all of them. <c>E25</c> or a whole number such as
/// <c>25</c> names an episode by its absolute number, and <c>E25-28</c> a range of them. The letters ignore case.
/// </para>
/// <para>
/// PowerShell converts a string to the identifier through the <see cref="string"/> operator, and an <see cref="int"/>
/// through the <see cref="int"/> one, as an absolute episode number. Any other value goes through the
/// <see cref="string"/> operator as its text. Text that doesn't parse gives the <see langword="default"/> identifier,
/// which names no episodes and isn't valid, so <c>Get-SonarrEpisode</c> stops with an error that lists it.
/// </para>
/// </remarks>
[DebuggerDisplay(@"{GetDebugString()}")]
public readonly struct SeasonEpisodeId : IEpisodeIdentifier
{
	readonly int _season;
	readonly bool _isAbsolute;
	readonly bool _isNotEmpty;
	readonly EpisodeRange _range;

	/// <summary>
	/// Gets the range of episode numbers.
	/// </summary>
	/// <value>
	/// The range of absolute episode numbers when <see cref="IsAbsolute"/> is <see langword="true"/>, or else the range
	/// of episode numbers in <see cref="Season"/>, which matches every episode when the identifier names none.
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
	/// <see langword="true"/> for the <see langword="default"/> identifier, which text that doesn't parse gives, for one
	/// whose season is less than 0, and for an absolute one whose first episode number isn't greater than 0; otherwise,
	/// <see langword="false"/>.
	/// </value>
	public bool IsEmpty => !_isNotEmpty;
	/// <summary>
	/// Gets the season number.
	/// </summary>
	/// <value>
	/// The number of the season that <see cref="EpisodeRange"/> is in, which matters only when <see cref="IsAbsolute"/>
	/// is <see langword="false"/>.
	/// </value>
	public int Season => _season;

	/// <summary>
	/// Initializes a new <see cref="SeasonEpisodeId"/> instance for the episode with the specified absolute number.
	/// </summary>
	/// <param name="absoluteNumber">The episode's absolute number.</param>
	private SeasonEpisodeId(int absoluteNumber)
	{
		_season = -1;
		_isAbsolute = true;
		_isNotEmpty = true;
		_range = new(absoluteNumber, absoluteNumber);
	}
	/// <summary>
	/// Initializes a new <see cref="SeasonEpisodeId"/> instance for a season and one episode number.
	/// </summary>
	/// <param name="season">
	/// The season number, which matters only when <paramref name="isAbsolute"/> is <see langword="false"/>, and names no
	/// episodes when it's less than 0.
	/// </param>
	/// <param name="episode">
	/// The episode number. A number less than 1 names every episode of the season, or no episode when
	/// <paramref name="isAbsolute"/> is <see langword="true"/>.
	/// </param>
	/// <param name="isAbsolute">
	/// <see langword="true"/> if <paramref name="episode"/> is an absolute episode number; otherwise,
	/// <see langword="false"/>.
	/// </param>
	private SeasonEpisodeId(int season, int episode, bool isAbsolute)
	{
		_season = season;
		_isAbsolute = isAbsolute;
		_isNotEmpty = isAbsolute ? episode > 0 : season >= 0;
		_range = new(episode, episode);
	}
	/// <summary>
	/// Initializes a new <see cref="SeasonEpisodeId"/> instance for a season and a range of episode numbers.
	/// </summary>
	/// <param name="season">
	/// The season number, which matters only when <paramref name="isAbsolute"/> is <see langword="false"/>, and names no
	/// episodes when it's less than 0.
	/// </param>
	/// <param name="epRange">
	/// The range of episode numbers. A range whose first number is 0 names every episode of the season, or no episode
	/// when <paramref name="isAbsolute"/> is <see langword="true"/>.
	/// </param>
	/// <param name="isAbsolute">
	/// <see langword="true"/> if <paramref name="epRange"/> holds absolute episode numbers; otherwise,
	/// <see langword="false"/>.
	/// </param>
	private SeasonEpisodeId(int season, EpisodeRange epRange, bool isAbsolute)
	{
		_season = season;
		_isAbsolute = isAbsolute;
		_isNotEmpty = isAbsolute ? epRange.Start > 0 : season >= 0;
		_range = epRange;
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
		return _isNotEmpty && this.EpisodeRange.IsValid();
	}

	/// <summary>
	/// Converts the text of an episode identifier to a <see cref="SeasonEpisodeId"/>, and returns a value that
	/// indicates whether it succeeded.
	/// </summary>
	/// <remarks>
	/// The text after <c>E</c> is the first episode number, and the number after a <c>-</c> there is the last. The text
	/// before <c>E</c> is <c>S</c> and the season number, or nothing for an absolute episode number, and a number
	/// without the <c>S</c>, as in <c>10E05</c>, doesn't parse. Text without an <c>E</c> is <c>S</c> and the season
	/// number, for the whole season, or a whole number, for an absolute episode number. The letters ignore case, and the
	/// numbers are read in the invariant culture.
	/// </remarks>
	/// <param name="value">The text, such as <c>S01E02</c>, <c>S01E02-05</c>, <c>S01</c>, <c>E25</c>, or <c>25</c>.</param>
	/// <param name="result">
	/// When the method returns, the identifier that <paramref name="value"/> names, or the <see langword="default"/>
	/// identifier when it doesn't parse.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="value"/> parses; otherwise, <see langword="false"/>, as when it's empty
	/// or white space.
	/// </returns>
	public static bool TryParse([ValidatedNotNull] ReadOnlySpan<char> value, out SeasonEpisodeId result)
	{
		result = default;
		if (value.IsWhiteSpace())
		{
			return false;
		}

		char e = 'e';
		int endEp = -1;
		int episode = -1;
		int index = value.IndexOf([e], StringComparison.InvariantCultureIgnoreCase);
		bool hasEpisodePart = index > -1;

		if (hasEpisodePart)
		{
			var epSlice = value.Slice(index + 1);
			int dashIndex = epSlice.IndexOf('-');
			if (dashIndex > -1)
			{
				var endSlice = epSlice.Slice(dashIndex + 1);
				if (int.TryParse(endSlice, CultureInfo.InvariantCulture, out int endEpNum))
				{
					endEp = endEpNum;
					epSlice = epSlice.Slice(0, dashIndex);
				}
			}

			if (int.TryParse(epSlice, CultureInfo.InvariantCulture, out int epNum))
			{
				episode = epNum;
			}

			value = value.Slice(0, index);
		}

		char s = 's';
		int season = 0;
		if (value.IsWhiteSpace())
		{
			result = endEp > -1
				? new(season, new EpisodeRange(episode, endEp), true)
				: new(season, episode, true);
			return true;
		}

		index = value.IndexOf(new ReadOnlySpan<char>(in s), StringComparison.InvariantCultureIgnoreCase);
		if (index > -1)
		{
			var sSlice = value.Slice(index + 1);

			if (int.TryParse(sSlice, CultureInfo.InvariantCulture, out season))
			{
				result = endEp > -1
					? new(season, new EpisodeRange(episode, endEp), false)
					: new(season, episode, false);

				return true;
			}
		}
		else if (!hasEpisodePart && int.TryParse(value, CultureInfo.InvariantCulture, out int absolute))
		{
			result = new(season, absolute, true);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Converts each element of an array of objects to a <see cref="SeasonEpisodeId"/>.
	/// </summary>
	/// <remarks>
	/// An identifier is copied as it is, an <see cref="int"/>, or a <see cref="long"/> in the range of an
	/// <see cref="int"/>, becomes an absolute episode number, and a string is parsed as
	/// <see cref="TryParse(ReadOnlySpan{char}, out SeasonEpisodeId)"/> parses it. Any other element,
	/// <see langword="null"/> included, becomes the <see langword="default"/> identifier.
	/// </remarks>
	/// <param name="array">The objects to convert.</param>
	/// <returns>
	/// A new array of the identifiers, in the same order, or an empty array when <paramref name="array"/> is empty.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is null.</exception>
	public static SeasonEpisodeId[] FromArray(object[] array)
	{
		ArgumentNullException.ThrowIfNull(array);
		if (array.Length <= 0)
		{
			return [];
		}

		SeasonEpisodeId[] copyTo = new SeasonEpisodeId[array.Length];

		for (int i = 0; i < array.Length; i++)
		{
			object? o = array[i];
			switch (o)
			{
				case SeasonEpisodeId sei:
					copyTo[i] = sei;
					break;

				case int intVal:
					copyTo[i] = intVal;
					break;

				case long longVal when longVal is <= int.MaxValue and >= int.MinValue:
					copyTo[i] = (int)longVal;
					break;

				case string strVal:
					copyTo[i] = strVal;
					break;

				default:
					copyTo[i] = default;
					break;
			}
		}

		return copyTo;
	}

	/// <summary>
	/// Gets the text that the debugger shows for the identifier.
	/// </summary>
	/// <remarks>
	/// The season shows as it's stored, even for an absolute identifier. The text is built on the stack.
	/// </remarks>
	/// <returns>The identifier in the form <c>S1E2</c>, or <c>S1E2-5</c> for a range.</returns>
	private string GetDebugString()
	{
		Span<char> span = stackalloc char[3 + (LengthConstants.INT_MAX * 2)];
		span[0] = 'S';
		this.Season.TryFormat(span.Slice(1), out int written, provider: CultureInfo.InvariantCulture);
		int pos = 1 + written;

		span[pos++] = 'E';
		_ = this.EpisodeRange.Start.TryFormat(span.Slice(pos), out written, provider: CultureInfo.InvariantCulture);
		pos += written;

		if (!this.EpisodeRange.IsSingle)
		{
			span[pos++] = '-';
			_ = this.EpisodeRange.End.TryFormat(span.Slice(pos), out written, provider: CultureInfo.InvariantCulture);
			pos += written;
		}

		return new string(span.Slice(0, pos));
	}

	/// <summary>
	/// Converts the text of an episode identifier to a <see cref="SeasonEpisodeId"/>.
	/// </summary>
	/// <param name="value">The text, such as <c>S01E02</c>, or <see langword="null"/>.</param>
	/// <returns>
	/// The identifier that <paramref name="value"/> names, or the <see langword="default"/> identifier when it's
	/// <see langword="null"/> or doesn't parse.
	/// </returns>
	public static implicit operator SeasonEpisodeId(string? value)
	{
		return TryParse(value, out var id) ? id : default;
	}
	/// <summary>
	/// Converts an absolute episode number to a <see cref="SeasonEpisodeId"/>.
	/// </summary>
	/// <param name="absoluteEpNumber">The episode's absolute number.</param>
	/// <returns>An absolute identifier for the one episode.</returns>
	public static implicit operator SeasonEpisodeId(int absoluteEpNumber)
	{
		return new(absoluteEpNumber);
	}
}
