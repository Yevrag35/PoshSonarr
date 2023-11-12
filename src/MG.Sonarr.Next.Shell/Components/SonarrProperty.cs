namespace MG.Sonarr.Next.Shell.Components
{
    public sealed record SonarrProperty : IComparable<SonarrProperty>
    {
        public required object? CurrentValue { get; init; }
        public bool IsReadOnly { get; init; }
        public required string Name { get; init; }
        public required string ObjectType { get; init; }
        public required string Type { get; init; }

        public int CompareTo(SonarrProperty? other)
        {
            int compare = this.ObjectType.CompareTo(other?.ObjectType);
            if (compare == 0)
            {
                compare = this.Name.CompareTo(other?.Name);
            }

            return compare;
        }
    }
}

