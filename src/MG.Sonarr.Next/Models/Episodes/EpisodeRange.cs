using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Extensions.Strings;

namespace MG.Sonarr.Next.Models.Episodes;

/// <summary>
/// Represents a range of episode numbers, including support for single episodes and matching all episodes.
/// </summary>
/// <remarks>An episode range can specify a single episode, a contiguous range of episodes, or all
/// episodes. If the range matches all episodes, both <see cref="Start"/> and <see cref="End"/> are set to 0. Use
/// <see cref="IsSingle"/> to determine if the range represents a single episode, and <see cref="AllMatch"/> to
/// check if all episodes are included. The range is inclusive of both <see cref="Start"/> and <see
/// cref="End"/>.</remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct EpisodeRange
{
	/// <summary>
	/// Gets the inclusive starting episode number of the range.
	/// </summary>
	public int Start { get; }
	/// <summary>
	/// Gets the inclusive ending episode number of the range.
	/// </summary>
	public int End { get; }
	/// <summary>
	/// Indicates whether the range matches all episodes.
	/// </summary>
	public bool AllMatch => this.Start == 0;
	/// <summary>
	/// Indicates whether the range represents a single episode.
	/// </summary>
	public bool IsSingle => !this.AllMatch && this.Start == this.End;

	/// <summary>
	/// Initializes a new instance of the EpisodeRange class with the specified start and end episode numbers.
	/// </summary>
	/// <remarks>If <paramref name="end"/> is less than <paramref name="start"/>, an
	/// ArgumentOutOfRangeException is thrown. Negative values for either parameter are automatically set to
	/// zero.</remarks>
	/// <param name="start">The zero-based index of the first episode in the range. If less than zero, the value is set to zero.</param>
	/// <param name="end">The zero-based index of the last episode in the range. Must be greater than or equal to <paramref
	/// name="start"/>. If less than zero, the value is set to zero.</param>
	public EpisodeRange(int start, int end)
	{
		this.Start = start < 0 ? 0 : start;
		this.End = end < 0 ? 0 : end;
	}

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
	public bool IsValid()
	{
		return this.End >= this.Start;
	}

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
