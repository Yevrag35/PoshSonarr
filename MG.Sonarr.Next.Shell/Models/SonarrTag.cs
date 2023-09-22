using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Shell.Models
{
    public sealed record SonarrTag : IHasId
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public required int Id { get; init; }
        public required string Label { get; init; }
    }
}
