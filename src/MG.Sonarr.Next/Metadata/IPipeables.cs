using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Shell.Models;

namespace MG.Sonarr.Next.Metadata
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

    public interface ILanguageProfilePipeable : IJsonMetadataTaggable
    {
        int LanguageProfileId { get; }
    }

    public interface IQualityProfilePipeable : IJsonMetadataTaggable
    {
        int QualityProfileId { get; }
    }

    public interface IReleasePipeableByEpisode : IJsonMetadataTaggable
    {
        int EpisodeId { get; }
    }

    public interface IReleasePipeableBySeries : IJsonMetadataTaggable
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

        void CommitTags();
        void Reset();
    }
}
