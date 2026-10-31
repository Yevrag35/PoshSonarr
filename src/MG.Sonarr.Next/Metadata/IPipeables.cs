using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Models;

namespace MG.Sonarr.Next.Metadata;

/// <summary>
/// Represents an object that can be piped to a cmdlet, which acts on the ID that the object gives.
/// </summary>
/// <remarks>
/// A cmdlet parameter typed as one of the <c>I...Pipeable</c> interfaces has its input checked through
/// <see cref="GetId"/> before the cmdlet sees it, so the ID that <see cref="GetId"/> returns is the one that the
/// interface's own property exposes.
/// </remarks>
/// <typeparam name="T">The pipeable interface that the ID is for, which lets a model give a different ID to each
/// interface that it implements.</typeparam>
public interface IPipeable<T> : IJsonSonarrMetadata
{
	/// <summary>
	/// Gets the ID that a cmdlet taking the object through <typeparamref name="T"/> acts on.
	/// </summary>
	/// <returns>
	/// The ID, or <see langword="null"/> when the object has none for <typeparamref name="T"/>.
	/// </returns>
	int? GetId();
}

///// <summary>
///// Defines a contract for types that provide a validatable identifier and support pipeable operations.
///// </summary>
///// <typeparam name="TPipeable">The type that implements both <see cref="IValidatableId{TPipeable}"/> and <see cref="IPipeable{TPipeable}"/>,
///// representing a pipeable entity with a validatable identifier.</typeparam>
//public interface IValidatableId<TPipeable> where TPipeable : IValidatableId<TPipeable>, IPipeable
//{
//	/// <summary>
//	/// Retrieves the identifier of the specified pipeable object for validation purposes.
//	/// </summary>
//	/// <param name="pipeable">The pipeable object from which to obtain the identifier. Cannot be null.</param>
//	/// <returns>The identifier of the pipeable object if available; otherwise, null.</returns>
//	static virtual int? GetValidatableId(TPipeable pipeable)
//	{
//		return pipeable.GetId();
//	}
//}

/// <summary>
/// Represents a series that can be piped to a cmdlet that gets the series' episodes from the <c>/episode</c>
/// endpoint.
/// </summary>
public interface IEpisodeBySeriesPipeable : IJsonSonarrMetadata,
	IPipeable<IEpisodeBySeriesPipeable>
	//IValidatableId<IEpisodeBySeriesPipeable>
{
	/// <summary>
	/// Gets the ID of the series.
	/// </summary>
	/// <value>The series' ID.</value>
	int SeriesId { get; }
	/// <summary>
	/// Gets the title of the series.
	/// </summary>
	/// <value>The series' display title.</value>
	string Title { get; }
}
/// <summary>
/// Represents an object that can be piped to a cmdlet that gets an episode from the <c>/episode</c> endpoint.
/// </summary>
public interface IEpisodePipeable : IJsonSonarrMetadata,
	IPipeable<IEpisodePipeable>
{
	/// <summary>
	/// Gets the ID of the episode.
	/// </summary>
	/// <value>The episode's ID.</value>
	int EpisodeId { get; }
}
/// <summary>
/// Represents an object that can be piped to a cmdlet that acts on an episode file through the <c>/episodefile</c>
/// endpoint.
/// </summary>
public interface IEpisodeFilePipeable : IJsonSonarrMetadata,
	IPipeable<IEpisodeFilePipeable>
	//IValidatableId<IEpisodeFilePipeable>
{
	/// <summary>
	/// Gets the ID of the episode file.
	/// </summary>
	/// <value>The episode file's ID.</value>
	int EpisodeFileId { get; }
}
/// <summary>
/// Represents a series that can be piped to a cmdlet that gets the series' episode files from the
/// <c>/episodefile</c> endpoint.
/// </summary>
public interface IEpisodeFileBySeriesPipeable : IJsonSonarrMetadata,
	IPipeable<IEpisodeFileBySeriesPipeable>
	//IValidatableId<IEpisodeFileBySeriesPipeable>
{
	/// <summary>
	/// Gets the ID of the series.
	/// </summary>
	/// <value>The series' ID.</value>
	int SeriesId { get; }
}
/// <summary>
/// Represents an object that can be piped to a cmdlet that gets a language profile from the
/// <c>/languageprofile</c> endpoint.
/// </summary>
public interface ILanguageProfilePipeable : IJsonMetadataTaggable,
	IPipeable<ILanguageProfilePipeable>
	//IValidatableId<ILanguageProfilePipeable>
{
	/// <summary>
	/// Gets the ID of the language profile.
	/// </summary>
	/// <value>The language profile's ID.</value>
	int LanguageProfileId { get; }
}
/// <summary>
/// Represents an object that can be piped to a cmdlet that gets a quality profile from the <c>/qualityprofile</c>
/// endpoint.
/// </summary>
public interface IQualityProfilePipeable : IJsonMetadataTaggable,
	IPipeable<IQualityProfilePipeable>
	//IValidatableId<IQualityProfilePipeable>
{
	/// <summary>
	/// Gets the ID of the quality profile.
	/// </summary>
	/// <value>The quality profile's ID.</value>
	int QualityProfileId { get; }
}
/// <summary>
/// Represents an episode that can be piped to a cmdlet that searches the <c>/release</c> endpoint for its releases.
/// </summary>
public interface IReleasePipeableByEpisode : IJsonMetadataTaggable,
	IPipeable<IReleasePipeableByEpisode>
	//IValidatableId<IReleasePipeableByEpisode>
{
	/// <summary>
	/// Gets the ID of the episode.
	/// </summary>
	/// <value>The episode's ID.</value>
	int EpisodeId { get; }
}
/// <summary>
/// Represents a series that can be piped to a cmdlet that searches the <c>/release</c> endpoint for its releases.
/// </summary>
public interface IReleasePipeableBySeries : IJsonMetadataTaggable,
	IPipeable<IReleasePipeableBySeries>
	//IValidatableId<IReleasePipeableBySeries>
{
	/// <summary>
	/// Gets the ID of the series.
	/// </summary>
	/// <value>The series' ID.</value>
	int SeriesId { get; }
}
/// <summary>
/// Represents an episode file that can be piped to a cmdlet that renames it.
/// </summary>
/// <remarks>
/// <see cref="IPipeable{T}.GetId"/> returns <see cref="EpisodeFileId"/>. <see cref="SeriesId"/> names the series
/// that the file's rename is sent for.
/// </remarks>
public interface IRenameFilePipeable : IJsonMetadataTaggable,
	IPipeable<IRenameFilePipeable>
	//IValidatableId<IRenameFilePipeable>
{
	/// <summary>
	/// Gets the ID of the episode file.
	/// </summary>
	/// <value>The episode file's ID.</value>
	int EpisodeFileId { get; }
	/// <summary>
	/// Gets the ID of the series that the episode file belongs to.
	/// </summary>
	/// <value>The series' ID.</value>
	int SeriesId { get; }
}
/// <summary>
/// Represents an object that can be piped to a cmdlet that acts on a series through the <c>/series</c> endpoint.
/// </summary>
public interface ISeriesPipeable : IJsonSonarrMetadata,
	IPipeable<ISeriesPipeable>
	//IValidatableId<ISeriesPipeable>
{
	/// <summary>
	/// Gets the ID of the series.
	/// </summary>
	/// <value>The series' ID.</value>
	int SeriesId { get; }
}
/// <summary>
/// Represents an object with tags that can be piped to the cmdlets that read and change its tags.
/// </summary>
public interface ITagPipeable : IHasId, IJsonSonarrMetadata,
	IPipeable<ITagPipeable>
	//IValidatableId<ITagPipeable>
{
	/// <summary>
	/// Gets a value that indicates whether a change to the object's tags must be saved with a request to Sonarr.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if the object exists on the server; <see langword="false"/> if it doesn't exist there
	/// yet, such as a lookup result or a schema, whose tags are sent with the request that creates it.
	/// </value>
	bool MustUpdateViaApi { get; }

	/// <summary>
	/// Gets the IDs of the object's tags.
	/// </summary>
	/// <value>The set of tag IDs, which a change to the object's tags edits in place.</value>
	ISet<int> Tags { get; }

	/// <summary>
	/// Makes the object's current tags the ones that <see cref="Reset"/> restores.
	/// </summary>
	/// <remarks>
	/// A cmdlet calls this after a request that saves the object's changed tags succeeds.
	/// </remarks>
	void CommitTags();
	/// <summary>
	/// Restores the tags that the object had when its tags were last committed.
	/// </summary>
	/// <remarks>
	/// A cmdlet calls this after a request that saves the object's changed tags fails.
	/// </remarks>
	void Reset();
}
/// <summary>
/// Represents a provider, such as a download client, an indexer, or a notification, that can be piped to the cmdlet
/// that tests it.
/// </summary>
public interface ITestPipeable : IJsonSonarrMetadata,
	IPipeable<ITestPipeable>
	//IValidatableId<ITestPipeable>
{
	/// <summary>
	/// Gets the ID of the provider.
	/// </summary>
	/// <value>The provider's ID.</value>
	int Id { get; }
}
