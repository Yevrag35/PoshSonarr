namespace MG.Sonarr.Next.Models.Episodes;

/// <summary>
/// Represents an inclusive range of episode numbers, which can be a single episode or every episode.
/// </summary>
/// <remarks>
/// <para>
/// A range made from its first and last episode numbers matches each episode number from the first to the last, both
/// included, so it's a single episode when the two are the same. A range can start at 0, and it then includes episode
/// 0. <see cref="All"/>, the range from 0 to <see cref="MG.Sonarr.Next.Numbers.PInt32.MaxValue"/>, matches every
/// episode.
/// </para>
/// <para>
/// The <see langword="default"/> range matches no episodes and isn't valid, so a range that was never made from
/// episode numbers can't widen a match.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct EpisodeRange
{
	/// <summary>
	/// Whether the range was made from episode numbers, which the <see langword="default"/> range wasn't.
	/// </summary>
	private readonly bool _isSet;

	/// <summary>
	/// Gets the range that matches every episode.
	/// </summary>
	/// <value>The range from 0 to <see cref="MG.Sonarr.Next.Numbers.PInt32.MaxValue"/>.</value>
	public static EpisodeRange All => new(0, pint.MaxValue);

	/// <summary>
	/// Gets the first episode number in the range.
	/// </summary>
	/// <value>The first episode number in the range, or 0 for the <see langword="default"/> range.</value>
	public pint Start { get; }
	/// <summary>
	/// Gets the last episode number in the range.
	/// </summary>
	/// <value>The last episode number in the range, or 0 for the <see langword="default"/> range.</value>
	public pint End { get; }
	/// <summary>
	/// Gets a value that indicates whether the range matches every episode.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if the range runs from 0 to <see cref="MG.Sonarr.Next.Numbers.PInt32.MaxValue"/>, as
	/// <see cref="All"/> does; otherwise, <see langword="false"/>.
	/// </value>
	public bool AllMatch => _isSet && this.Start == 0 && this.End == pint.MaxValue;
	/// <summary>
	/// Gets a value that indicates whether the range is a single episode.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if the range was made from two episode numbers that are the same; otherwise,
	/// <see langword="false"/>, as for the <see langword="default"/> range.
	/// </value>
	public bool IsSingle => _isSet && this.Start == this.End;
	/// <summary>
	/// Gets a value that indicates whether the range was made from episode numbers.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if a constructor or <see cref="All"/> made the range; otherwise, <see langword="false"/>
	/// for the <see langword="default"/> range.
	/// </value>
	internal bool IsSet => _isSet;

	/// <summary>
	/// Initializes a new <see cref="EpisodeRange"/> instance from its first and last episode numbers.
	/// </summary>
	/// <remarks>
	/// The range keeps an <paramref name="end"/> less than <paramref name="start"/>, and <see cref="IsValid"/>
	/// reports such a range.
	/// </remarks>
	/// <param name="start">The first episode number in the range, 0 or greater.</param>
	/// <param name="end">The last episode number in the range, 0 or greater.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="start"/> or <paramref name="end"/> is negative.
	/// </exception>
	public EpisodeRange(int start, int end)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(start);
		ArgumentOutOfRangeException.ThrowIfNegative(end);
		_isSet = true;
		this.Start = new pint(start, true);
		this.End = new pint(end, true);
	}

	/// <summary>
	/// Determines whether an episode number is in the range.
	/// </summary>
	/// <param name="episodeNumber">The episode number to test.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="episodeNumber"/> is between <see cref="Start"/> and
	/// <see cref="End"/>, inclusive; otherwise, <see langword="false"/>, as it always is for the
	/// <see langword="default"/> range and for one that ends before it starts.
	/// </returns>
	public bool IsInRange(int episodeNumber)
	{
		return _isSet && episodeNumber >= this.Start && episodeNumber <= this.End;
	}
	/// <summary>
	/// Determines whether the range was made from episode numbers and doesn't end before it starts.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if the range isn't the <see langword="default"/> one and <see cref="End"/> is greater
	/// than or equal to <see cref="Start"/>; otherwise, <see langword="false"/>.
	/// </returns>
	public bool IsValid()
	{
		return _isSet && this.End >= this.Start;
	}

	/// <summary>
	/// Returns a string that describes the range.
	/// </summary>
	/// <returns>
	/// <c>All Match</c> if the range matches every episode, <c>No Match</c> for the <see langword="default"/> range,
	/// and otherwise the range in the form <c>Start = 1;End = 5</c>.
	/// </returns>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in nameof()")]
	public override string ToString()
	{
		const string START = "Start";
		const string END = "End";
		const int max_length = 5 + 3 + 8 + (LengthConstants.INT_MAX * 2);

		if (this.AllMatch)
		{
			return "All Match";
		}
		else if (!_isSet)
		{
			return "No Match";
		}

		ReadOnlySpan<char> separator = [' ', '=', ' '];

		using SpanStringBuilder builder = new(stackalloc char[max_length]);
		builder.Append(START);
		builder.Append(separator);
		builder.Append(this.Start, LengthConstants.INT_MAX);
		builder.Append(';');
		builder.Append(END);
		builder.Append(separator);
		builder.Append(this.End, LengthConstants.INT_MAX);

		return builder.ToString();
	}
}
