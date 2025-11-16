using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Json;

namespace MG.Sonarr.Next.Metadata
{
    /// <summary>
    /// Defines a contract for types that support pipeable operations and provide Sonarr metadata in JSON format.
    /// </summary>
    /// <typeparam name="TSelf">The type that implements the IPipeable interface.</typeparam>
    public interface IPipeable<TSelf> : IJsonSonarrMetadata where TSelf : IPipeable<TSelf>
    {
        /// <summary>
        /// Gets the unique identifier of the implementation to pass along to PowerShell piping.
        /// </summary>
        /// <returns>
        /// The unique identifier of the implementation, or <see langword="null"/> if not applicable.
        /// </returns>
        int? GetId();
    }

    /// <summary>
    /// Defines a contract for types that provide a validatable identifier and support pipeable operations.
    /// </summary>
    /// <typeparam name="TPipeable">The type that implements both <see cref="IValidatableId{TPipeable}"/> and <see cref="IPipeable{TPipeable}"/>,
    /// representing a pipeable entity with a validatable identifier.</typeparam>
    public interface IValidatableId<TPipeable> where TPipeable : IValidatableId<TPipeable>, IPipeable<TPipeable>
    {
        /// <summary>
        /// Retrieves the identifier of the specified pipeable object for validation purposes.
        /// </summary>
        /// <param name="pipeable">The pipeable object from which to obtain the identifier. Cannot be null.</param>
        /// <returns>The identifier of the pipeable object if available; otherwise, null.</returns>
        static virtual int? GetValidatableId(TPipeable pipeable)
        {
            return pipeable.GetId();
        }
    }

    /// <summary>
    /// An interface exposing the SeriesId property for piping to the /episode endpoint.
    /// </summary>
    public interface IEpisodeBySeriesPipeable : IJsonSonarrMetadata,
        IPipeable<IEpisodeBySeriesPipeable>,
        IValidatableId<IEpisodeBySeriesPipeable>
    {
        /// <summary>
        /// The series ID of the implementation.
        /// </summary>
        int SeriesId { get; }
        /// <summary>
        /// The display title of the implementation.
        /// </summary>
        string Title { get; }
    }
    /// <summary>
    /// An interface exposing the EpisodeId property for piping to the /episode endpoint.
    /// </summary>
    public interface IEpisodePipeable : IJsonSonarrMetadata,
        IPipeable<IEpisodePipeable>,
        IValidatableId<IEpisodePipeable>
    {
        /// <summary>
        /// The episode ID of the implementation.
        /// </summary>
        int EpisodeId { get; }
    }
    /// <summary>
    /// An interface exposing the EpisodeFileId property for piping to the /episodefile endpoint.
    /// </summary>
    public interface IEpisodeFilePipeable : IJsonSonarrMetadata,
        IPipeable<IEpisodeFilePipeable>,
        IValidatableId<IEpisodeFilePipeable>
    {
        /// <summary>
        /// The episode file ID of the implementation.
        /// </summary>
        int EpisodeFileId { get; }
    }
    /// <summary>
    /// An interface exposing the SeriesId property for piping to the /episodefile endpoint.
    /// </summary>
    public interface IEpisodeFileBySeriesPipeable : IJsonSonarrMetadata,
        IPipeable<IEpisodeFileBySeriesPipeable>,
        IValidatableId<IEpisodeFileBySeriesPipeable>
    {
        /// <summary>
        /// The series ID of the implementation.
        /// </summary>
        int SeriesId { get; }
    }
    /// <summary>
    /// An interface exposing the LanguageProfileId property for piping to the /languageprofile endpoint.
    /// </summary>
    public interface ILanguageProfilePipeable : IJsonMetadataTaggable,
        IPipeable<ILanguageProfilePipeable>,
        IValidatableId<ILanguageProfilePipeable>
    {
        /// <summary>
        /// The language profile ID of the implementation.
        /// </summary>
        int LanguageProfileId { get; }
    }
    /// <summary>
    /// An interface exposing the QualityProfileId property for piping to the /qualityprofile endpoint.
    /// </summary>
    public interface IQualityProfilePipeable : IJsonMetadataTaggable,
        IPipeable<IQualityProfilePipeable>,
        IValidatableId<IQualityProfilePipeable>
    {
        /// <summary>
        /// The quality profile ID of the implementation.
        /// </summary>
        int QualityProfileId { get; }
    }
    /// <summary>
    /// An interface exposing the EpisodeId property for piping to the /release endpoint.
    /// </summary>
    public interface IReleasePipeableByEpisode : IJsonMetadataTaggable,
        IPipeable<IReleasePipeableByEpisode>,
        IValidatableId<IReleasePipeableByEpisode>
    {
        /// <summary>
        /// The episode ID of the implementation.
        /// </summary>
        int EpisodeId { get; }
    }
    /// <summary>
    /// An inteface exposing the SeriesId property for piping to the /release endpoint.
    /// </summary>
    public interface IReleasePipeableBySeries : IJsonMetadataTaggable,
        IPipeable<IReleasePipeableBySeries>,
        IValidatableId<IReleasePipeableBySeries>
    {
        /// <summary>
        /// The series ID of the implementation.
        /// </summary>
        int SeriesId { get; }
    }
    /// <summary>
    /// Defines a contract for a pipeable operation that renames an episode file within a series, supporting metadata
    /// tagging and validation.
    /// </summary>
    /// <remarks>Implementations of this interface are intended to participate in processing pipelines that
    /// handle file renaming tasks. The interface also supports JSON metadata tagging and validation of identifiers,
    /// enabling integration with systems that require these capabilities.</remarks>
    public interface IRenameFilePipeable : IJsonMetadataTaggable,
        IPipeable<IRenameFilePipeable>,
        IValidatableId<IRenameFilePipeable>
    {
        /// <summary>
        /// Gets the episode file ID of the implementation.
        /// </summary>
        int EpisodeFileId { get; }
        /// <summary>
        /// Gets the series ID of the implementation.
        /// </summary>
        int SeriesId { get; }
    }
    /// <summary>
    /// An interface exposing the SeriesId property for piping to the /series endpoint.
    /// </summary>
    public interface ISeriesPipeable : IJsonSonarrMetadata,
        IPipeable<ISeriesPipeable>,
        IValidatableId<ISeriesPipeable>
    {
        /// <summary>
        /// The series ID of the implementation.
        /// </summary>
        int SeriesId { get; }
    }
    /// <summary>
    /// An interface exposing properties and methods for piping to the /tag endpoint.
    /// </summary>
    public interface ITagPipeable : IHasId, IJsonSonarrMetadata,
        IPipeable<ITagPipeable>,
        IValidatableId<ITagPipeable>
    {
        /// <summary>
        /// Indicates that tag changes must be saved via a Sonarr API request.
        /// </summary>
        /// <remarks>
        ///     This will be <see langword="false"/> for objects that have not been created yet on the Sonarr server side 
        ///     or are simply scaffolding (schema) objects.
        /// </remarks>
        bool MustUpdateViaApi { get; }

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
    /// <summary>
    /// Represents a pipeable test entity that provides Sonarr metadata and supports validation by identifier.
    /// </summary>
    /// <remarks>Implementations of this interface can be used in data processing pipelines and must provide a
    /// unique identifier via the <see cref="Id"/> property. This interface extends metadata and validation capabilities
    /// for test entities within Sonarr-related workflows.</remarks>
    public interface ITestPipeable : IJsonSonarrMetadata,
        IPipeable<ITestPipeable>,
        IValidatableId<ITestPipeable>
    {
        /// <summary>
        /// Gets the unique identifier of the implementation.
        /// </summary>
        int Id { get; }
    }
}
