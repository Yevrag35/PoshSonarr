using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.PSProperties;

namespace MG.Sonarr.Next.Models.Episodes;

/// <summary>
/// Represents an episode of a series.
/// </summary>
/// <remarks>
/// <para>
/// The object's <see cref="IdSonarrObject{TSelf}.Id"/> is the ID that it pipes as an episode to search releases for.
/// It pipes its <see cref="EpisodeFileId"/> as an episode file and as a file to rename, and its
/// <see cref="SeriesId"/> as a series.
/// </para>
/// <para>
/// The numbers and IDs are copies made when the object is deserialized, and don't follow a change that PowerShell
/// makes to the properties afterward. The <c>AirDate</c> property is held back from PowerShell. When Sonarr sent one,
/// <see cref="OnSerializing"/> writes it into a request body, and <see cref="Commit"/> and <see cref="Reset"/> remove
/// it again.
/// </para>
/// <para>
/// Episodes sort by series ID, then by season number, episode number, and absolute episode number.
/// </para>
/// </remarks>
[SonarrObject]
public sealed class EpisodeObject : IdSonarrObject<EpisodeObject>,
	IEpisodeFilePipeable,
	IJsonOnSerializing,
	IReleasePipeableByEpisode,
	IRenameFilePipeable,
	ISeriesPipeable,
	ISerializableNames<EpisodeObject>
{
	/// <summary>
	/// The number of properties that an episode is created with room for.
	/// </summary>
	private const int CAPACITY = 25;
	/// <summary>
	/// The PowerShell type name of <see cref="EpisodeObject"/>.
	/// </summary>
	private static readonly string _typeName = typeof(EpisodeObject).GetName();
	/// <summary>
	/// The name of the property that holds the episode's air date.
	/// </summary>
	private const string AIR_DATE = "AirDate";
	/// <summary>
	/// The episode's air date, held back from PowerShell until the object is serialized.
	/// </summary>
	private DateOnly _airDate;
	/// <summary>
	/// Whether Sonarr sent an air date, which <see cref="_airDate"/> then holds.
	/// </summary>
	private bool _hasAirDate;
	/// <summary>
	/// The series title or ID that <see cref="Series"/> returns.
	/// </summary>
	private IComparable? _manuallySetSeries;

	/// <summary>
	/// Gets the episode's absolute number.
	/// </summary>
	/// <value>The episode's number counted across all seasons, or 0 if Sonarr sent none.</value>
	public int AbsoluteEpisodeNumber { get; private set; }
	/// <summary>
	/// Gets the ID of the episode to search releases for.
	/// </summary>
	/// <value>The episode's <see cref="IdSonarrObject{TSelf}.Id"/>.</value>
	int IReleasePipeableByEpisode.EpisodeId => this.Id;
	/// <summary>
	/// Gets the ID of the episode's file.
	/// </summary>
	/// <value>The ID of the episode's file, or 0 if the episode has none.</value>
	public int EpisodeFileId { get; private set; }
	/// <summary>
	/// Gets the episode's number in its season.
	/// </summary>
	/// <value>The episode's number in its season.</value>
	public int EpisodeNumber { get; private set; }
	/// <summary>
	/// Gets a value that indicates whether the episode has an absolute number.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if Sonarr sent an absolute episode number; otherwise, <see langword="false"/>.
	/// </value>
	public bool HasAbsolute { get; private set; }
	/// <summary>
	/// Gets the number of the episode's season.
	/// </summary>
	/// <value>The episode's season number.</value>
	public int SeasonNumber { get; private set; }
	/// <summary>
	/// Gets the value that identifies the episode's series when episodes are sorted.
	/// </summary>
	/// <remarks>
	/// It's the title of the nested series when Sonarr sent one, or else the series ID, until
	/// <see cref="SetSeries(IEpisodeBySeriesPipeable)"/> replaces it.
	/// </remarks>
	/// <value>A <see cref="string"/> title or an <see cref="int"/> series ID.</value>
	public IComparable Series => _manuallySetSeries ?? this.SeriesId;
	/// <summary>
	/// Gets the ID of the episode's series.
	/// </summary>
	/// <value>The ID of the series that the episode belongs to.</value>
	public int SeriesId { get; private set; }

	/// <summary>
	/// Initializes a new <see cref="EpisodeObject"/> instance with no properties.
	/// </summary>
	public EpisodeObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Compares the episode with another by series ID, then season number, episode number, and absolute episode
	/// number.
	/// </summary>
	/// <remarks>
	/// The series compare by ID alone, the ID that <see cref="SetSeries(IEpisodeBySeriesPipeable)"/> set or else
	/// <see cref="SeriesId"/>, so a series' title takes no part, and the same episode compares as 0 with itself whether
	/// Sonarr sent it with its series or without.
	/// </remarks>
	/// <param name="other">The episode to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this episode comes first, 0 if both sort the same, and a positive number if this episode
	/// comes after <paramref name="other"/> or <paramref name="other"/> is <see langword="null"/>.
	/// </returns>
	public override int CompareTo(EpisodeObject? other)
	{
		if (other is null)
		{
			return 1;
		}

		int compare = this.GetSeriesSortId().CompareTo(other.GetSeriesSortId());
		if (compare != 0)
		{
			return compare;
		}

		compare = this.SeasonNumber.CompareTo(other.SeasonNumber);
		if (compare == 0)
		{
			compare = this.EpisodeNumber.CompareTo(other.EpisodeNumber);
			if (compare == 0)
			{
				compare = this.AbsoluteEpisodeNumber.CompareTo(other.AbsoluteEpisodeNumber);
			}
		}

		return compare;
	}
	/// <summary>
	/// Gets the series ID that the episode sorts by.
	/// </summary>
	/// <returns>
	/// The ID that <see cref="SetSeries(IEpisodeBySeriesPipeable)"/> or deserialization set, or else
	/// <see cref="SeriesId"/>.
	/// </returns>
	private int GetSeriesSortId()
	{
		return _manuallySetSeries is int id ? id : this.SeriesId;
	}

	/// <summary>
	/// Gets the metadata tag of the episode endpoint.
	/// </summary>
	/// <param name="resolver">The resolver that holds the tags.</param>
	/// <param name="existing">The tag that the object already has, which the episode doesn't use.</param>
	/// <returns>The <see cref="Meta.Episode"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.Episode];
	}

	/// <summary>
	/// Determines whether the episode is one that an identifier names.
	/// </summary>
	/// <param name="identifier">The season and episode range, or the absolute range, to test the episode against.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="identifier"/> is absolute and the episode has an absolute number in its
	/// range, or if <paramref name="identifier"/> isn't absolute and the episode is in its season and has an episode
	/// number in its range; otherwise, <see langword="false"/>.
	/// </returns>
	public bool Matches(IEpisodeIdentifier identifier)
	{
		return identifier.IsAbsolute
			? this.HasAbsolute && identifier.EpisodeRange.IsInRange(this.AbsoluteEpisodeNumber)
			: identifier.EpisodeRange.IsInRange(this.EpisodeNumber) && this.SeasonNumber == identifier.Season;
	}

	/// <summary>
	/// Holds the <c>AirDate</c> property back from PowerShell and copies the properties that the C# members keep.
	/// </summary>
	/// <param name="alreadyCalled">
	/// <see langword="true"/> if the object has been deserialized before; otherwise, <see langword="false"/>.
	/// </param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		base.OnDeserialized(alreadyCalled);
		if (this.TryGetProperty(AIR_DATE, out DateOnly airDate))
		{
			_airDate = airDate;
			_hasAirDate = true;
			this.Properties.Remove(AIR_DATE);
		}

		if (this.TryGetProperty(nameof(this.AbsoluteEpisodeNumber), out int abId))
		{
			this.HasAbsolute = true;
		}

		this.AbsoluteEpisodeNumber = abId;

		if (this.TryGetProperty(nameof(this.EpisodeFileId), out int epFileId))
		{
			this.EpisodeFileId = epFileId;
		}

		if (this.TryGetProperty(nameof(this.EpisodeNumber), out int epNumber))
		{
			this.EpisodeNumber = epNumber;
		}

		if (this.TryGetProperty(nameof(this.SeasonNumber), out int seasonNumber))
		{
			this.SeasonNumber = seasonNumber;
		}

		if (this.TryGetProperty(nameof(this.SeriesId), out int seriesId))
		{
			this.SeriesId = seriesId;
		}

		if (this.TryGetNonNullProperty(nameof(this.Series), out SeriesObject? so))
		{
			_manuallySetSeries = so.Title;
		}
		else
		{
			_manuallySetSeries = this.SeriesId;
		}
	}

	/// <summary>
	/// Removes the <c>AirDate</c> property that <see cref="OnSerializing"/> added, after a request that sent the
	/// episode succeeds.
	/// </summary>
	/// <remarks>
	/// The nested series that Sonarr sends with an episode from <c>/episode/{id}</c> is written by its own converter,
	/// which runs its <see cref="SeriesObject.OnSerializing"/>, so the nested series is committed too.
	/// </remarks>
	public override void Commit()
	{
		this.RemoveAirDate();
		this.GetNestedSeries()?.Commit();
		base.Commit();
	}
	/// <summary>
	/// Adds the <c>AirDate</c> property back to the object before it's serialized, when Sonarr sent one.
	/// </summary>
	public void OnSerializing()
	{
		if (_hasAirDate)
		{
			this.Properties.Add(StructNoteProperty.Create(AIR_DATE, _airDate));
		}
	}
	/// <summary>
	/// Removes the <c>AirDate</c> property that <see cref="OnSerializing"/> added, after a request that sent the
	/// episode fails.
	/// </summary>
	/// <remarks>
	/// The nested series that Sonarr sends with an episode from <c>/episode/{id}</c> is written by its own converter,
	/// which runs its <see cref="SeriesObject.OnSerializing"/>, so the nested series is reset too.
	/// </remarks>
	public override void Reset()
	{
		this.RemoveAirDate();
		this.GetNestedSeries()?.Reset();
		base.Reset();
	}
	/// <summary>
	/// Gets the series that Sonarr nested in the episode.
	/// </summary>
	/// <returns>The value of the <c>Series</c> property when it's a <see cref="SeriesObject"/>; otherwise,
	/// <see langword="null"/>.</returns>
	private SeriesObject? GetNestedSeries()
	{
		return this.Properties[nameof(this.Series)]?.Value as SeriesObject;
	}
	/// <summary>
	/// Removes the <c>AirDate</c> property that <see cref="OnSerializing"/> added, when Sonarr sent an air date.
	/// </summary>
	/// <remarks>
	/// When Sonarr sent none, <see cref="OnSerializing"/> added nothing, so a property of that name, such as one that
	/// Sonarr sent as <see langword="null"/>, stays.
	/// </remarks>
	private void RemoveAirDate()
	{
		if (_hasAirDate)
		{
			this.Properties.Remove(AIR_DATE);
		}
	}

	/// <summary>
	/// Sets the series title or ID that the episode sorts by.
	/// </summary>
	/// <remarks>
	/// The title is used when it isn't empty or white space, and otherwise the series ID when it's greater than 0. When
	/// <paramref name="series"/> has neither, <see cref="Series"/> keeps its value.
	/// </remarks>
	/// <param name="series">The series that the episode belongs to.</param>
	public void SetSeries(IEpisodeBySeriesPipeable series)
	{
		if (!string.IsNullOrWhiteSpace(series.Title))
		{
			_manuallySetSeries = series.Title;
		}
		else if (series.SeriesId > 0)
		{
			_manuallySetSeries = series.SeriesId;
		}
	}

	/// <summary>
	/// Inserts the <see cref="EpisodeObject"/> type name at the start of the object's PowerShell type names.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}

	/// <summary>
	/// Gets the ID that the episode pipes as an episode file.
	/// </summary>
	/// <returns>The <see cref="EpisodeFileId"/>.</returns>
	int? IPipeable<IEpisodeFilePipeable>.GetId()
	{
		return this.EpisodeFileId;
	}
	/// <summary>
	/// Gets the ID that the episode pipes as an episode to search releases for.
	/// </summary>
	/// <returns>The episode's <see cref="IdSonarrObject{TSelf}.Id"/>.</returns>
	int? IPipeable<IReleasePipeableByEpisode>.GetId()
	{
		return this.Id;
	}
	/// <summary>
	/// Gets the ID that the episode pipes as a file to rename.
	/// </summary>
	/// <returns>The <see cref="EpisodeFileId"/>.</returns>
	int? IPipeable<IRenameFilePipeable>.GetId()
	{
		return this.EpisodeFileId;
	}
	/// <summary>
	/// Gets the ID that the episode pipes as a series.
	/// </summary>
	/// <returns>The <see cref="SeriesId"/>.</returns>
	int? IPipeable<ISeriesPipeable>.GetId()
	{
		return this.SeriesId;
	}
}
