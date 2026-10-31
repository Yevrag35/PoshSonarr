using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Episodes;

/// <summary>
/// Represents an episode file on disk that Sonarr tracks.
/// </summary>
/// <remarks>
/// The object's <see cref="IdSonarrObject{TSelf}.Id"/> is the ID that it pipes as an episode file and as a file to
/// rename, and it pipes its <see cref="SeriesId"/> as a series. <see cref="SeriesId"/> is a copy made when the object
/// is deserialized, and doesn't follow a change that PowerShell makes to the property afterward.
/// </remarks>
[SonarrObject]
public sealed class EpisodeFileObject : IdSonarrObject<EpisodeFileObject>,
	IEpisodeFilePipeable,
	IHasId,
	IRenameFilePipeable,
	ISeriesPipeable,
	ISerializableNames<EpisodeFileObject>
{
	/// <summary>
	/// The number of properties that an episode file is created with room for.
	/// </summary>
	const int CAPACITY = 14;
	/// <summary>
	/// The PowerShell type name of <see cref="EpisodeFileObject"/>.
	/// </summary>
	static readonly string _typeName = typeof(EpisodeFileObject).GetName();

	/// <summary>
	/// Gets the ID of the episode file.
	/// </summary>
	/// <value>The file's <see cref="IdSonarrObject{TSelf}.Id"/>.</value>
	int IEpisodeFilePipeable.EpisodeFileId => this.Id;
	/// <summary>
	/// Gets the ID of the file to rename.
	/// </summary>
	/// <value>The file's <see cref="IdSonarrObject{TSelf}.Id"/>.</value>
	int IRenameFilePipeable.EpisodeFileId => this.Id;
	/// <summary>
	/// Gets the ID of the file's series.
	/// </summary>
	/// <value>The ID of the series that the file belongs to.</value>
	public int SeriesId { get; private set; }

	/// <summary>
	/// Initializes a new <see cref="EpisodeFileObject"/> instance with no properties.
	/// </summary>
	public EpisodeFileObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Gets the metadata tag of the episode file endpoint.
	/// </summary>
	/// <param name="resolver">The resolver that holds the tags.</param>
	/// <param name="existing">The tag that the object already has, which the episode file doesn't use.</param>
	/// <returns>The <see cref="Meta.EPISODE_FILE"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.EPISODE_FILE];
	}

	/// <summary>
	/// Copies the <c>SeriesId</c> property into <see cref="SeriesId"/>.
	/// </summary>
	/// <param name="alreadyCalled">
	/// <see langword="true"/> if the object has been deserialized before; otherwise, <see langword="false"/>.
	/// </param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		base.OnDeserialized(alreadyCalled);

		if (this.TryGetProperty(nameof(this.SeriesId), out int seriesId))
		{
			this.SeriesId = seriesId;
		}
	}

	/// <summary>
	/// Inserts the <see cref="EpisodeFileObject"/> type name at the start of the object's PowerShell type names.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}

	/// <summary>
	/// Gets the ID that the episode file pipes as an episode file.
	/// </summary>
	/// <returns>The file's <see cref="IdSonarrObject{TSelf}.Id"/>.</returns>
	int? IPipeable<IEpisodeFilePipeable>.GetId()
	{
		return this.Id;
	}
	/// <summary>
	/// Gets the ID that the episode file pipes as a file to rename.
	/// </summary>
	/// <returns>The file's <see cref="IdSonarrObject{TSelf}.Id"/>.</returns>
	int? IPipeable<IRenameFilePipeable>.GetId()
	{
		return this.Id;
	}
	/// <summary>
	/// Gets the ID that the episode file pipes as a series.
	/// </summary>
	/// <returns>The <see cref="SeriesId"/>.</returns>
	int? IPipeable<ISeriesPipeable>.GetId()
	{
		return this.SeriesId;
	}
}
