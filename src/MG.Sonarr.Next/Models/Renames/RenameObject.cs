using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Renames;

/// <summary>
/// Represents an episode file that a rename would change, from the <c>/rename</c> endpoint.
/// </summary>
/// <remarks>
/// The object pipes its series ID as an <see cref="ISeriesPipeable"/> and its episode file ID as an
/// <see cref="IRenameFilePipeable"/>, and writes both IDs back to a request body. Rename objects sort by series ID,
/// then by episode file ID.
/// </remarks>
[SonarrObject]
public sealed class RenameObject : SonarrObject,
	IComparable<RenameObject>,
	IJsonOnSerializing,
	ISeriesPipeable,
	IRenameFilePipeable,
	ISerializableNames<RenameObject>
{
	/// <summary>
	/// The number of properties that a new instance makes room for.
	/// </summary>
	const int CAPACITY = 6;
	/// <summary>
	/// The type's full name, which <see cref="SetPSTypeName"/> puts first in the PowerShell type names.
	/// </summary>
	static readonly string _typeName = typeof(RenameObject).GetName();

	/// <summary>
	/// Gets the ID of the episode file that the rename changes.
	/// </summary>
	/// <value>
	/// A copy of the <c>EpisodeFileId</c> property, taken when the object is deserialized, or 0 before then.
	/// </value>
	public int EpisodeFileId { get; private set; }
	/// <summary>
	/// Gets the ID of the series that the episode file belongs to.
	/// </summary>
	/// <value>A copy of the <c>SeriesId</c> property, taken when the object is deserialized, or 0 before then.</value>
	public int SeriesId { get; private set; }

	/// <summary>
	/// Initializes a new <see cref="RenameObject"/> instance with no properties, which the JSON converter fills.
	/// </summary>
	public RenameObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Compares this rename object with another by series ID, then by episode file ID.
	/// </summary>
	/// <param name="other">The rename object to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this object sorts before <paramref name="other"/>, 0 if they sort together, or a
	/// positive number if it sorts after <paramref name="other"/> or <paramref name="other"/> is
	/// <see langword="null"/>.
	/// </returns>
	public int CompareTo(RenameObject? other)
	{
		int compare = Comparer<int?>.Default.Compare(this.SeriesId, other?.SeriesId);
		if (compare == 0)
		{
			compare = Comparer<int?>.Default.Compare(this.EpisodeFileId, other?.EpisodeFileId);
		}

		return compare;
	}
	/// <summary>
	/// Gets the rename tag from a resolver.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which this method ignores.</param>
	/// <returns>The resolver's <see cref="Meta.RENAMABLE"/> tag.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.RENAMABLE];
	}

	/// <summary>
	/// Copies the series and episode file IDs from their properties.
	/// </summary>
	/// <param name="alreadyCalled">
	/// <see langword="true"/> if the object was deserialized before; otherwise, <see langword="false"/>.
	/// </param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		if (this.TryGetProperty(nameof(this.SeriesId), out int seriesId))
		{
			this.SeriesId = seriesId;
		}

		if (this.TryGetProperty(nameof(this.EpisodeFileId), out int episodeFileId))
		{
			this.EpisodeFileId = episodeFileId;
		}
	}

	/// <summary>
	/// Writes the series and episode file IDs to their properties before the object is written to a request body.
	/// </summary>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Because of 'nameof()'")]
	public void OnSerializing()
	{
		this.UpdateProperty(this.EpisodeFileId, propertyName: nameof(EpisodeFileId));
		this.UpdateProperty(this.SeriesId, propertyName: nameof(SeriesId));
	}
	/// <summary>
	/// Puts the type's full name first in the object's PowerShell type names, ahead of the name that the base class
	/// adds.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}

	/// <summary>
	/// Gets the ID that the object pipes as an <see cref="ISeriesPipeable"/>.
	/// </summary>
	/// <returns>The <see cref="SeriesId"/>.</returns>
	int? IPipeable<ISeriesPipeable>.GetId()
	{
		return this.SeriesId;
	}
	/// <summary>
	/// Gets the ID that the object pipes as an <see cref="IRenameFilePipeable"/>.
	/// </summary>
	/// <returns>The <see cref="EpisodeFileId"/>.</returns>
	int? IPipeable<IRenameFilePipeable>.GetId()
	{
		return this.EpisodeFileId;
	}
}
