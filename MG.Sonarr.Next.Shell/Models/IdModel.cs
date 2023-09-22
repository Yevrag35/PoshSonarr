namespace MG.Sonarr.Next.Shell.Models
{
    public sealed record IdModel : IHasId
    {
        public required int Id { get; init; }
    }
}
