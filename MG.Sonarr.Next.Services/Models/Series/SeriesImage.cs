namespace MG.Sonarr.Next.Shell.Models.Series
{
    public sealed record SeriesImage : IComparable<SeriesImage>
    {
        public required string CoverType { get; init; }
        public required string Url { get; init; }

        public int CompareTo(SeriesImage? other)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(this.CoverType, other?.CoverType);
        }
    }
}
