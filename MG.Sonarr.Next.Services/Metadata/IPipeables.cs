using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Shell.Models;

namespace MG.Sonarr.Next.Services.Metadata
{
    public interface IEpisodePipeable : IJsonSonarrMetadata
    {
        int SeriesId { get; }
    }

    public interface ISeriesPipeable : IJsonSonarrMetadata
    {
        int SeriesId { get; }
    }

    public interface ITagPipeable : IHasId, IJsonSonarrMetadata
    {
        ISet<int> Tags { get; }
    }
}
