using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Models;

namespace MG.Sonarr.Next.Shell.Output
{
    public interface IIdTagOuptut : IHasId, IJsonSonarrMetadata
    {
        SortedSet<int> Tags { get; }
    }
    public interface ITagOutput : IJsonSonarrMetadata
    {
        SortedSet<int> Tags { get; }
    }
}
