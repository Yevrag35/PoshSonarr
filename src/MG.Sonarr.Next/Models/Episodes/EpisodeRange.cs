using MG.Sonarr.Next.Buffers;

namespace MG.Sonarr.Next.Models.Episodes;

/// <summary>
/// Represents an inclusive range of episode numbers, which can be a single episode or every episode.
/// </summary>
/// <remarks>
/// A range whose <see cref="Start"/> is 0 matches every episode, whatever its <see cref="End"/>. A range whose
/// <see cref="Start"/> and <see cref="End"/> are the same number other than 0 is a single episode. Both ends of the
/// range are included.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct EpisodeRange
{
	/// <summary>
	/// Gets the first episode number in the range.
	/// </summary>
	/// <value>The first episode number in the range, or 0 when the range matches every episode.</value>
	public int Start { get; }
	/// <summary>
	/// Gets the last episode number in the range.
	/// </summary>
	/// <value>The last episode number in the range.</value>
	public int End { get; }
	/// <summary>
	/// Gets a value that indicates whether the range matches every episode.
	/// </summary>
	/// <value><see langword="true"/> if <see cref="Start"/> is 0; otherwise, <see langword="false"/>.</value>
	public bool AllMatch => this.Start == 0;
	/// <summary>
	/// Gets a value that indicates whether the range is a single episode.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if <see cref="Start"/> and <see cref="End"/> are the same number other than 0;
	/// otherwise, <see langword="false"/>.
	/// </value>
	public bool IsSingle => !this.AllMatch && this.Start == this.End;

	/// <summary>
	/// Initializes a new <see cref="EpisodeRange"/> instance from its first and last episode numbers.
	/// </summary>
	/// <remarks>
	/// A negative number becomes 0. The constructor accepts an <paramref name="end"/> less than
	/// <paramref name="start"/>, and <see cref="IsValid"/> reports such a range.
	/// </remarks>
	/// <param name="start">The first episode number in the range, or 0 to match every episode.</param>
	/// <param name="end">The last episode number in the range.</param>
	public EpisodeRange(int start, int end)
	{
		this.Start = start < 0 ? 0 : start;
		this.End = end < 0 ? 0 : end;
	}

	/// <summary>
	/// Determines whether an episode number is in the range.
	/// </summary>
	/// <param name="episodeNumber">The episode number to test.</param>
	/// <returns>
	/// <see langword="true"/> if the range matches every episode, or <paramref name="episodeNumber"/> is between
	/// <see cref="Start"/> and <see cref="End"/>, inclusive; otherwise, <see langword="false"/>.
	/// </returns>
	public bool IsInRange(int episodeNumber)
	{
		if (this.AllMatch)
		{
			return true;
		}

		return this.IsSingle
			? episodeNumber == this.Start
			: episodeNumber >= this.Start && episodeNumber <= this.End;
	}
	/// <summary>
	/// Determines whether the range's last episode number isn't less than its first.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if <see cref="End"/> is greater than or equal to <see cref="Start"/>; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	public bool IsValid()
	{
		return this.End >= this.Start;
	}

	/// <summary>
	/// Returns a string that describes the range.
	/// </summary>
	/// <returns>
	/// <c>All Match</c> if the range matches every episode; otherwise, the range in the form
	/// <c>Start = 1;End = 5</c>.
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
