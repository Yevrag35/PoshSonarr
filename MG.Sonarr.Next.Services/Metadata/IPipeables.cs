using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Shell.Models;

namespace MG.Sonarr.Next.Services.Metadata
{
    public interface IEpisodeBySeriesPipeable : IJsonSonarrMetadata
    {
        int SeriesId { get; }
    }

    public interface IEpisodePipeable : IJsonSonarrMetadata
    {
        int EpisodeId { get; }
    }

    public interface IEpisodeFilePipeable : IJsonSonarrMetadata
    {
        int EpisodeFileId { get; }
    }

    public interface IEpisodeFileBySeriesPipeable : IJsonSonarrMetadata
    {
        int SeriesId { get; }
    }

    public interface IQualityProfilePipeable : IJsonMetadataTaggable
    {
        int QualityProfileId { get; }
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
