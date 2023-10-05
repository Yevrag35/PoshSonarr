namespace MG.Sonarr.Next.Services.Models
{
    public sealed record SonarrServerError
    {
        public required string Message { get; init; }
        public string? Description { get; init; }
    }
}
