using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.ManualImports;
using MG.Sonarr.Next.PSProperties;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Series;

/// <summary>
/// Represents a series in Sonarr's library.
/// </summary>
/// <remarks>
/// <para>
/// The series pipes its ID as a series, and as a series whose episodes, episode files, or releases a cmdlet gets. It
/// pipes its <see cref="LanguageProfileId"/> and <see cref="QualityProfileId"/> as profiles.
/// </para>
/// <para>
/// The <c>FirstAired</c> property is held back from PowerShell. <see cref="OnSerializing"/> writes it into a request
/// body, and a commit or reset removes it again. A <c>ShortOverview</c> property shows the overview: an alias of
/// <c>Overview</c> when it's 90 characters or fewer, or else a read-only copy cut after a word and ended with
/// <c>...</c>.
/// </para>
/// <para>
/// The JSON fields <c>imdbId</c> and <c>seasonFolder</c> become the properties <c>IMDbId</c> and
/// <c>UseSeasonFolders</c>, and the <c>SeriesType</c> and <c>Status</c> values get an uppercase first letter. The
/// object's PowerShell type names include <c>SeriesBase</c>, which it shares with <see cref="AddSeriesObject"/>.
/// </para>
/// </remarks>
[SonarrObject]
public class SeriesObject : TagUpdateObject<SeriesObject>,
	IEpisodeBySeriesPipeable,
	IEpisodeFileBySeriesPipeable,
	IHasId,
	IJsonOnSerializing,
	ILanguageProfilePipeable,
	IQualityProfilePipeable,
	IReleasePipeableBySeries,
	ISerializableNames<SeriesObject>,
	ISeriesPipeable
	//IValidatableId<IEpisodeBySeriesPipeable>
{
	/// <summary>
	/// The number of properties that a series is created with room for.
	/// </summary>
	const int CAPACITY = 46;
	/// <summary>
	/// The name of the property that holds the date that the series first aired.
	/// </summary>
	private const string FIRST_AIRED = "FirstAired";
	/// <summary>
	/// The name of the property that holds the series' overview.
	/// </summary>
	private const string OVERVIEW = "Overview";
	/// <summary>
	/// The most characters that the short overview has before it's cut.
	/// </summary>
	private const int SHORT_OVERVIEW_LENGTH = 90;
	/// <summary>
	/// The PowerShell type name of <see cref="SeriesObject"/>.
	/// </summary>
	static readonly string _typeName = typeof(SeriesObject).GetName();
	/// <summary>
	/// The date that the series first aired, held back from PowerShell until the object is serialized.
	/// </summary>
	private DateOnly _firstAired;

	/// <summary>
	/// Gets or sets the ID of the series' language profile.
	/// </summary>
	/// <value>The value of the <c>LanguageProfileId</c> property, or 0 if it has none.</value>
	public int LanguageProfileId
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}
	/// <summary>
	/// Gets or sets the ID of the series' quality profile.
	/// </summary>
	/// <value>The value of the <c>QualityProfileId</c> property, or 0 if it has none.</value>
	public int QualityProfileId
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}
	/// <summary>
	/// Gets the ID of the series whose episodes a cmdlet gets.
	/// </summary>
	/// <value>The series' ID.</value>
	int IEpisodeBySeriesPipeable.SeriesId => this.SeriesId;
	/// <summary>
	/// Gets the ID of the series whose episode files a cmdlet gets.
	/// </summary>
	/// <value>The series' ID.</value>
	int IEpisodeFileBySeriesPipeable.SeriesId => this.SeriesId;
	/// <summary>
	/// Gets the ID of the series whose releases a cmdlet gets.
	/// </summary>
	/// <value>The series' ID.</value>
	int IReleasePipeableBySeries.SeriesId => this.SeriesId;
	/// <summary>
	/// Gets the ID of the series.
	/// </summary>
	/// <value>The series' ID.</value>
	int ISeriesPipeable.SeriesId => this.SeriesId;
	/// <summary>
	/// Gets the ID of the series.
	/// </summary>
	/// <value>The series' <see cref="IdSonarrObject{TSelf}.Id"/>.</value>
	protected int SeriesId => this.Id;
	/// <summary>
	/// Gets or sets the series' title.
	/// </summary>
	/// <remarks>
	/// The title is a copy of the <c>Title</c> property made when the object is deserialized, and doesn't follow a
	/// change that PowerShell makes to the property afterward.
	/// </remarks>
	/// <value>The series' title, or an empty string if Sonarr sent none.</value>
	public string Title { get; set; } = string.Empty;

	/// <summary>
	/// Initializes a new <see cref="SeriesObject"/> instance with no properties.
	/// </summary>
	public SeriesObject()
		: this(CAPACITY)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="SeriesObject"/> instance with room for a number of properties.
	/// </summary>
	/// <param name="capacity">The number of properties that the object is created with room for.</param>
	protected SeriesObject(int capacity)
		: base(capacity)
	{
	}

	/// <summary>
	/// Removes the <c>FirstAired</c> property after a request that sent the series succeeds.
	/// </summary>
	protected override void OnCommit()
	{
		this.Properties.Remove(FIRST_AIRED);
		base.OnCommit();
	}

	/// <summary>
	/// Gets the metadata tag of the series endpoint.
	/// </summary>
	/// <param name="resolver">The resolver that holds the tags.</param>
	/// <param name="existing">The tag that the object already has, which the series doesn't use.</param>
	/// <returns>The <see cref="Meta.SERIES"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.SERIES];
	}

	/// <summary>
	/// Holds the <c>FirstAired</c> property back from PowerShell, copies the title, and adds the short overview.
	/// </summary>
	/// <param name="alreadyCalled">
	/// <see langword="true"/> if the object has been deserialized before; otherwise, <see langword="false"/>.
	/// </param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		base.OnDeserialized(alreadyCalled);

		PSPropertyInfo? property = this.Properties[FIRST_AIRED];
		if (property is not null && property.Value is DateOnly dateOnly)
		{
			_firstAired = dateOnly;
			this.Properties.Remove(property.Name);
		}

		if (this.TryGetProperty(nameof(this.QualityProfileId), out int profileId))
		{
			this.QualityProfileId = profileId;
		}

		if (this.TryGetNonNullProperty(nameof(this.Title), out string? title))
		{
			this.Title = title;
		}

		if (this.TryGetProperty(OVERVIEW, out string? overview))
		{
			this.TruncateOverview(overview);
		}
	}
	/// <summary>
	/// Adds the <c>FirstAired</c> property back to the object before it's serialized.
	/// </summary>
	public virtual void OnSerializing()
	{
		this.SetValue(_firstAired, FIRST_AIRED);
	}
	/// <summary>
	/// Removes the <c>FirstAired</c> property after a request that sent the series fails.
	/// </summary>
	protected override void OnReset()
	{
		this.Properties.Remove(FIRST_AIRED);
		base.OnReset();
	}
	/// <summary>
	/// Inserts the <see cref="SeriesObject"/> type name, followed by <c>SeriesBase</c>, at the start of the object's
	/// PowerShell type names.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
		this.TypeNames.Insert(1, "SeriesBase");
	}
	/// <summary>
	/// Determines whether the series is read-only as a property of a parent object.
	/// </summary>
	/// <param name="propertyName">The name of the parent's property that holds the series.</param>
	/// <param name="parentType">The type of the parent object.</param>
	/// <returns>
	/// <see langword="false"/> if the series is the <c>Series</c> property of a <see cref="ManualImportObject"/>, which
	/// the user sets before the import is posted; otherwise, <see langword="true"/>.
	/// </returns>
	internal override bool ShouldBeReadOnly(string propertyName, Type parentType)
	{
		if (Constants.PROPERTY_SERIES == propertyName && parentType.Equals(typeof(ManualImportObject)))
		{
			return false;
		}

		return base.ShouldBeReadOnly(propertyName, parentType);
	}

	/// <summary>
	/// Adds the <c>ShortOverview</c> property, unless the object already has one.
	/// </summary>
	/// <remarks>
	/// An overview that's empty, white space, or 90 characters or fewer gets an alias of <c>Overview</c>. A longer one
	/// gets a read-only copy cut after a word and ended with <c>...</c>.
	/// </remarks>
	/// <param name="overview">The series' overview, or <see langword="null"/>.</param>
	private void TruncateOverview(string? overview)
	{
		PSPropertyInfo? existing = this.Properties[Constants.PROPERTY_SHORT_OVERVIEW];
		if (existing is not null)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(overview) || overview.Length <= SHORT_OVERVIEW_LENGTH)
		{
			this.Properties.Add(new PSAliasProperty(Constants.PROPERTY_SHORT_OVERVIEW, OVERVIEW));
			return;
		}

		ShortOverview shorted = ShortOverview.FromValue(overview, SHORT_OVERVIEW_LENGTH);
		this.Properties.Add(new ReadOnlyStringProperty(Constants.PROPERTY_SHORT_OVERVIEW, shorted.Text));
	}

	/// <summary>
	/// The number of property names to capitalize.
	/// </summary>
	const int DICT_CAPACITY = 2;
	/// <summary>
	/// The maps between JSON field names and property names, built when first used.
	/// </summary>
	private protected static readonly Lazy<JsonNameHolder> _names = new(GetJsonNames);
	/// <summary>
	/// The names of the properties whose values get an uppercase first letter.
	/// </summary>
	private static readonly HashSet<string> _capitalProps = new(DICT_CAPACITY)
	{
		"SeriesType", "Status",
	};

	/// <summary>
	/// Builds the maps between JSON field names and property names.
	/// </summary>
	/// <returns>The maps that rename <c>ImdbId</c> to <c>IMDbId</c> and <c>SeasonFolder</c> to <c>UseSeasonFolders</c>.</returns>
	private static JsonNameHolder GetJsonNames()
	{
		return JsonNameHolder
			.FromDeserializationNamePairs(new KeyValuePair<string, string>[]
			{
				new("ImdbId", "IMDbId"),
				new("SeasonFolder", "UseSeasonFolders"),
			});
	}

	/// <summary>
	/// Gets the property names that JSON field names are read as.
	/// </summary>
	/// <returns>A map from <c>ImdbId</c> to <c>IMDbId</c> and from <c>SeasonFolder</c> to <c>UseSeasonFolders</c>.</returns>
	public static IReadOnlyDictionary<string, string> GetDeserializedNames()
	{
		return _names.Value.DeserializationNames;
	}
	/// <summary>
	/// Gets the names of the string properties whose values get an uppercase first letter.
	/// </summary>
	/// <returns>A set that holds <c>SeriesType</c> and <c>Status</c>.</returns>
	public static IReadOnlySet<string> GetPropertiesToCapitalize()
	{
		return _capitalProps;
	}
	/// <summary>
	/// Gets the JSON field names that property names are written as.
	/// </summary>
	/// <returns>A map from <c>IMDbId</c> to <c>ImdbId</c> and from <c>UseSeasonFolders</c> to <c>SeasonFolder</c>.</returns>
	public static IReadOnlyDictionary<string, string> GetSerializedNames()
	{
		return _names.Value.SerializationNames;
	}

	/// <summary>
	/// Gets the ID that the series pipes as a series.
	/// </summary>
	/// <returns>The series' <see cref="IdSonarrObject{TSelf}.Id"/>.</returns>
	private protected virtual int? GetSeriesId()
	{
		return this.Id;
	}
	/// <summary>
	/// Gets the ID that the series pipes as a series whose episodes a cmdlet gets.
	/// </summary>
	/// <returns>The ID that <see cref="GetSeriesId"/> returns.</returns>
	int? IPipeable<IEpisodeBySeriesPipeable>.GetId()
	{
		return this.GetSeriesId();
	}
	/// <summary>
	/// Gets the ID that the series pipes as a series whose episode files a cmdlet gets.
	/// </summary>
	/// <returns>The ID that <see cref="GetSeriesId"/> returns.</returns>
	int? IPipeable<IEpisodeFileBySeriesPipeable>.GetId()
	{
		return this.GetSeriesId();
	}
	/// <summary>
	/// Gets the ID that the series pipes as a language profile.
	/// </summary>
	/// <returns>The <see cref="LanguageProfileId"/>.</returns>
	int? IPipeable<ILanguageProfilePipeable>.GetId()
	{
		return this.LanguageProfileId;
	}
	/// <summary>
	/// Gets the ID that the series pipes as a quality profile.
	/// </summary>
	/// <returns>The <see cref="QualityProfileId"/>.</returns>
	int? IPipeable<IQualityProfilePipeable>.GetId()
	{
		return this.QualityProfileId;
	}
	/// <summary>
	/// Gets the ID that the series pipes as a series whose releases a cmdlet gets.
	/// </summary>
	/// <returns>The ID that <see cref="GetSeriesId"/> returns.</returns>
	int? IPipeable<IReleasePipeableBySeries>.GetId()
	{
		return this.GetSeriesId();
	}
	/// <summary>
	/// Gets the ID that the series pipes as a series.
	/// </summary>
	/// <returns>The ID that <see cref="GetSeriesId"/> returns.</returns>
	int? IPipeable<ISeriesPipeable>.GetId()
	{
		return this.GetSeriesId();
	}
}
