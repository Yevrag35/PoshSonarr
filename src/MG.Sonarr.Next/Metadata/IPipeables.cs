using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Json;

namespace MG.Sonarr.Next.Metadata
{
    /// <summary>
    /// An interface exposing the SeriesId property for piping to the /episode endpoint.
    /// </summary>
    public interface IEpisodeBySeriesPipeable : IJsonSonarrMetadata
    {
        /// <summary>
        /// The series ID of the implementation.
        /// </summary>
        int SeriesId { get; }
    }
    /// <summary>
    /// An interface exposing the EpisodeId property for piping to the /episode endpoint.
    /// </summary>
    public interface IEpisodePipeable : IJsonSonarrMetadata
    {
        /// <summary>
        /// The episode ID of the implementation.
        /// </summary>
        int EpisodeId { get; }
    }
    /// <summary>
    /// An interface exposing the EpisodeFileId property for piping to the /episodefile endpoint.
    /// </summary>
    public interface IEpisodeFilePipeable : IJsonSonarrMetadata
    {
        /// <summary>
        /// The episode file ID of the implementation.
        /// </summary>
        int EpisodeFileId { get; }
    }
    /// <summary>
    /// An interface exposing the SeriesId property for piping to the /episodefile endpoint.
    /// </summary>
    public interface IEpisodeFileBySeriesPipeable : IJsonSonarrMetadata
    {
        /// <summary>
        /// The series ID of the implementation.
        /// </summary>
        int SeriesId { get; }
    }
    /// <summary>
    /// An interface exposing the LanguageProfileId property for piping to the /languageprofile endpoint.
    /// </summary>
    public interface ILanguageProfilePipeable : IJsonMetadataTaggable
    {
        /// <summary>
        /// The language profile ID of the implementation.
        /// </summary>
        int LanguageProfileId { get; }
    }
    /// <summary>
    /// An interface exposing the QualityProfileId property for piping to the /qualityprofile endpoint.
    /// </summary>
    public interface IQualityProfilePipeable : IJsonMetadataTaggable
    {
        /// <summary>
        /// The quality profile ID of the implementation.
        /// </summary>
        int QualityProfileId { get; }
    }
    /// <summary>
    /// An interface exposing the EpisodeId property for piping to the /release endpoint.
    /// </summary>
    public interface IReleasePipeableByEpisode : IJsonMetadataTaggable
    {
        /// <summary>
        /// The episode ID of the implementation.
        /// </summary>
        int EpisodeId { get; }
    }
    /// <summary>
    /// An inteface exposing the SeriesId property for piping to the /release endpoint.
    /// </summary>
    public interface IReleasePipeableBySeries : IJsonMetadataTaggable
    {
        /// <summary>
        /// The series ID of the implementation.
        /// </summary>
        int SeriesId { get; }
    }
    public interface IRenameFilePipeable : IJsonMetadataTaggable
    {
        int EpisodeFileId { get; }
        int SeriesId { get; }
    }
    /// <summary>
    /// An interface exposing the SeriesId property for piping to the /series endpoint.
    /// </summary>
    public interface ISeriesPipeable : IJsonSonarrMetadata
    {
        /// <summary>
        /// The series ID of the implementation.
        /// </summary>
        int SeriesId { get; }
    }
    /// <summary>
    /// An interface exposing properties and methods for piping to the /tag endpoint.
    /// </summary>
    public interface ITagPipeable : IHasId, IJsonSonarrMetadata
    {
        /// <summary>
        /// Gets the unique set of tag IDs of the implementation.
        /// </summary>
        ISet<int> Tags { get; }

        /// <summary>
        /// Instructs the implementation to commit any changes that may have occurred.
        /// </summary>
        /// <remarks>
        ///     For instance, if a tag was successfully added to an object and an API request was successfully sent,
        ///     the implementation should commit that pending change.
        /// </remarks>
        void CommitTags();
        /// <summary>
        /// Instructs the implementation to revert any changes that may have occurred.
        /// </summary>
        /// <remarks>
        ///     For instance, if a tag was "unsuccessfully" removed during an API call, the implementation should
        ///     re-add the tag ID back before any changes were made.
        /// </remarks>
        void Reset();
    }
}
