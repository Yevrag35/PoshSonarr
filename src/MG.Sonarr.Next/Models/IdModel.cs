namespace MG.Sonarr.Next.Models
{
    public sealed record IdModel : IHasId
    {
        public required int Id { get; init; }
    }
}
