using MG.Sonarr.Next.Json;

namespace MG.Sonarr.Next.Models
{
    public interface ISchemaObject : IJsonSonarrMetadata
    {
        bool IsTaggable { get; }
    }
}
