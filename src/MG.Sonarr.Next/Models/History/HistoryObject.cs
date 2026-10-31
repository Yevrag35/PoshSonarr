using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.History;

/// <summary>
/// Represents a record in the Sonarr server's history, such as a grab, an import, or a failed download, from
/// <c>/history</c>.
/// </summary>
/// <remarks>
/// The record's nested <c>Data</c> holds the details of the event that it records.
/// </remarks>
[SonarrObject]
public sealed class HistoryObject : IdSonarrObject<HistoryObject>,
	ISerializableNames<HistoryObject>
{
	/// <summary>
	/// The number of properties that a new history record reserves room for.
	/// </summary>
	const int CAPACITY = 14;
	/// <summary>
	/// The full name of the type, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(HistoryObject).GetName();

	/// <summary>
	/// Gets the ID that the download client gave the download that the record is about.
	/// </summary>
	/// <remarks>
	/// It's a copy of the <c>DownloadId</c> property, taken while the record is deserialized.
	/// </remarks>
	/// <value>The download's ID, or an empty string when the record has none.</value>
	public string DownloadId { get; private set; } = string.Empty;
	/// <summary>
	/// Gets the ID of the episode that the record is about.
	/// </summary>
	/// <remarks>
	/// It's a copy of the <c>EpisodeId</c> property, taken while the record is deserialized.
	/// </remarks>
	/// <value>The episode's ID, or 0 when the record has none.</value>
	public int EpisodeId { get; private set; }
	/// <summary>
	/// Gets the ID of the series that the record is about.
	/// </summary>
	/// <remarks>
	/// It's a copy of the <c>SeriesId</c> property, taken while the record is deserialized.
	/// </remarks>
	/// <value>The series' ID, or 0 when the record has none.</value>
	public int SeriesId { get; private set; }

	/// <summary>
	/// Initializes a new <see cref="HistoryObject"/> instance with no properties.
	/// </summary>
	public HistoryObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Gets the metadata tag for history records.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's tags.</param>
	/// <param name="existing">The tag that the object has now, which a history record doesn't use.</param>
	/// <returns>The resolver's tag for the history endpoint.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.HISTORY];
	}
	/// <summary>
	/// Copies the record's download, episode, and series IDs into <see cref="DownloadId"/>, <see cref="EpisodeId"/>,
	/// and <see cref="SeriesId"/>.
	/// </summary>
	/// <param name="alreadyCalled">
	/// <see langword="true"/> if the object has been deserialized before; otherwise, <see langword="false"/>.
	/// </param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		base.OnDeserialized(alreadyCalled);
		if (this.TryGetNonNullProperty(nameof(this.DownloadId), out string? dlId))
		{
			this.DownloadId = dlId;
		}

		if (this.TryGetProperty(nameof(this.EpisodeId), out int epId))
		{
			this.EpisodeId = epId;
		}

		if (this.TryGetProperty(nameof(this.SeriesId), out int seriesId))
		{
			this.SeriesId = seriesId;
		}
	}
	/// <summary>
	/// Inserts the record's own type name ahead of the base type names.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}
}
