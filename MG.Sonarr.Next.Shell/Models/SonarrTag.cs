namespace MG.Sonarr.Next.Shell.Models
{
    public sealed record SonarrTag : IHasId
    {
        public required int Id { get; init; }
        public required string Label { get; init; }
    }
}
