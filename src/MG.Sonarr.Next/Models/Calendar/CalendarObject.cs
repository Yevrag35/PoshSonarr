using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Calendar;

/// <summary>
/// Represents an episode that airs in the date range of a calendar request.
/// </summary>
/// <remarks>
/// <para>
/// The object's <see cref="IdSonarrObject{TSelf}.Id"/> is the episode's ID. It pipes that ID as an episode, its
/// <see cref="EpisodeFileId"/> as an episode file and a file to rename, and its <see cref="SeriesId"/> as a series.
/// </para>
/// <para>
/// The object removes the <c>AirDate</c> property and keeps <c>AirDateUtc</c>, and calendar entries sort by
/// <see cref="AirDateUtc"/> and then by ID. The C# properties are copies made when the object is deserialized, and
/// don't follow a change that PowerShell makes to the properties afterward.
/// </para>
/// </remarks>
[SonarrObject]
public sealed class CalendarObject : IdSonarrObject<CalendarObject>,
	IEpisodeFilePipeable,
	IEpisodePipeable,
	IRenameFilePipeable,
	ISeriesPipeable,
	ISerializableNames<CalendarObject>,
	ITagResolvable<CalendarObject>
{
	/// <summary>
	/// The number of properties that a calendar entry is created with room for.
	/// </summary>
	const int CAPACITY = 20;
	/// <summary>
	/// The PowerShell type name of <see cref="CalendarObject"/>.
	/// </summary>
	static readonly string _typeName = typeof(CalendarObject).GetName();
	/// <summary>
	/// Gets the time that the episode airs, in UTC.
	/// </summary>
	/// <value>The episode's air time, or <see langword="default"/> if Sonarr sent none.</value>
	public DateTimeOffset AirDateUtc { get; private set; }
	/// <summary>
	/// Gets the ID of the episode's file.
	/// </summary>
	/// <value>The ID of the episode's file, or 0 if the episode has none.</value>
	public int EpisodeFileId { get; private set; }
	/// <summary>
	/// Gets the episode's ID.
	/// </summary>
	/// <value>The calendar entry's <see cref="IdSonarrObject{TSelf}.Id"/>.</value>
	int IEpisodePipeable.EpisodeId => this.Id;
	/// <summary>
	/// Gets the ID of the episode's series.
	/// </summary>
	/// <value>The ID of the episode's series.</value>
	public int SeriesId { get; private set; }

	/// <summary>
	/// Initializes a new <see cref="CalendarObject"/> instance with no properties.
	/// </summary>
	public CalendarObject()
		: base(CAPACITY)
	{
	}

	//BUG: CompareTo returns -1 for a null other, where IComparable<T> requires a positive number, so null sorts after every entry instead of before.
	/// <summary>
	/// Compares the calendar entry with another by air time and then by ID.
	/// </summary>
	/// <param name="other">The calendar entry to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this entry airs first, or airs at the same time and has the lower ID; 0 if both have the
	/// same air time and ID; otherwise, a positive number, as when <paramref name="other"/> is
	/// <see langword="null"/>.
	/// </returns>
	public override int CompareTo(CalendarObject? other)
	{
		if (other is null)
		{
			return -1;
		}

		int compare = this.AirDateUtc.CompareTo(other.AirDateUtc);
		if (compare == 0)
		{
			compare = this.Id.CompareTo(other.Id);
		}

		return compare;
	}

	/// <summary>
	/// Gets the metadata tag of the calendar endpoint.
	/// </summary>
	/// <param name="resolver">The resolver that holds the tags.</param>
	/// <param name="existing">The tag that the object already has, which the calendar entry doesn't use.</param>
	/// <returns>The <see cref="Meta.CALENDAR"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return GetTag(resolver);
	}

	/// <summary>
	/// Gets the metadata tag of the calendar endpoint without an instance.
	/// </summary>
	/// <param name="resolver">The resolver that holds the tags.</param>
	/// <returns>The <see cref="Meta.CALENDAR"/> tag from <paramref name="resolver"/>.</returns>
	public static MetadataTag GetTag(IMetadataResolver resolver)
	{
		return resolver[Meta.CALENDAR];
	}

	/// <summary>
	/// Removes the <c>AirDate</c> property and copies the properties that the C# members keep.
	/// </summary>
	/// <param name="alreadyCalled">
	/// <see langword="true"/> if the object has been deserialized before; otherwise, <see langword="false"/>.
	/// </param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		base.OnDeserialized(alreadyCalled);
		this.Properties.Remove("AirDate");

		if (this.TryGetProperty(nameof(this.SeriesId), out int seriesId))
		{
			this.SeriesId = seriesId;
		}

		if (this.TryGetProperty(nameof(this.AirDateUtc), out DateTimeOffset airDateUtc))
		{
			this.AirDateUtc = airDateUtc;
		}

		if (this.TryGetProperty(nameof(this.EpisodeFileId), out int epFileId))
		{
			this.EpisodeFileId = epFileId;
		}
	}
	/// <summary>
	/// Inserts the <see cref="CalendarObject"/> type name at the start of the object's PowerShell type names.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}

	/// <summary>
	/// Gets the ID that the calendar entry pipes as an episode.
	/// </summary>
	/// <returns>The episode's ID.</returns>
	int? IPipeable<IEpisodePipeable>.GetId()
	{
		return this.Id;
	}
	/// <summary>
	/// Gets the ID that the calendar entry pipes as an episode file.
	/// </summary>
	/// <returns>The <see cref="EpisodeFileId"/>.</returns>
	int? IPipeable<IEpisodeFilePipeable>.GetId()
	{
		return this.EpisodeFileId;
	}
	/// <summary>
	/// Gets the ID that the calendar entry pipes as a series.
	/// </summary>
	/// <returns>The <see cref="SeriesId"/>.</returns>
	int? IPipeable<ISeriesPipeable>.GetId()
	{
		return this.SeriesId;
	}
	/// <summary>
	/// Gets the ID that the calendar entry pipes as a file to rename.
	/// </summary>
	/// <returns>The <see cref="EpisodeFileId"/>.</returns>
	int? IPipeable<IRenameFilePipeable>.GetId()
	{
		return this.EpisodeFileId;
	}
}
