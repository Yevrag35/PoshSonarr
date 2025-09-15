using MG.Sonarr.Next.Extensions.Strings;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Models.Episodes
{
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
            if (this.AllMatch)
            {
                return "All Match";
            }

            ReadOnlySpan<char> startName = nameof(Start);
            ReadOnlySpan<char> endName = nameof(End);

            int length = startName.Length + endName.Length
                + 8
                + (LengthConstants.INT_MAX * 2);

            Span<char> span = stackalloc char[length];
            ReadOnlySpan<char> separator = [' ', '=', ' '];

            int position = 0;
            startName.CopyToSlice(span, ref position);
            separator.CopyToSlice(span, ref position);

            _ = this.Start.TryFormat(span.Slice(position), out int written);
            position += written;

            span[position++] = ';';
            endName.CopyToSlice(span, ref position);
            separator.CopyToSlice(span, ref position);

            _ = this.End.TryFormat(span.Slice(position), out written);
            position += written;

            return new string(span.Slice(0, position));
        }
    }
}
