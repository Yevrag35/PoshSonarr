using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.PSProperties;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Series;

/// <summary>
/// Represents a series from a lookup that can be added to Sonarr's library.
/// </summary>
/// <remarks>
/// <para>
/// The series isn't in the library yet, so it has no ID: it removes the <c>Id</c> and <c>Added</c> properties, and
/// it pipes no series ID. Its <c>LanguageProfileId</c> is a read-only 1, and it uses season folders when the lookup
/// lists more than one season.
/// </para>
/// <para>
/// <see cref="OnSerializing"/> writes <see cref="AddOptions"/>, the title, and <see cref="Path"/> into the request
/// body that adds the series, the path as <c>Path</c> when <see cref="IsFullPath"/> is <see langword="true"/> and as
/// <c>RootFolderPath</c> otherwise. A reset removes the path property and clears <see cref="AddOptions"/>.
/// </para>
/// <para>
/// Lookup results sort by title, ignoring case. The object's first PowerShell type name replaces
/// <see cref="SeriesObject"/>'s, and it keeps <c>SeriesBase</c>.
/// </para>
/// </remarks>
[SonarrObject]
public sealed class AddSeriesObject : SeriesObject,
	IComparable<AddSeriesObject>,
	IJsonOnSerializing,
	ISerializableNames<AddSeriesObject>
{
	/// <summary>
	/// The number of properties that a lookup result is created with room for.
	/// </summary>
	const int CAPACITY = 50;
	/// <summary>
	/// The name of the property that holds the root folder that the series is added under.
	/// </summary>
	const string ROOT_FOLDER_PATH = "RootFolderPath";
	/// <summary>
	/// The PowerShell type name of <see cref="AddSeriesObject"/>.
	/// </summary>
	static readonly string _typeName = typeof(AddSeriesObject).GetName();
	/// <summary>
	/// The path that <see cref="Path"/> returns.
	/// </summary>
	private string? _path;
	/// <summary>
	/// The name of the property that the path was last written to, or <see langword="null"/> if it hasn't been.
	/// </summary>
	private string? _pathProp;

	/// <summary>
	/// Gets or sets the options that Sonarr applies when it adds the series.
	/// </summary>
	/// <value>The options to add the series with, or <see langword="null"/> for none.</value>
	public SeriesAddOptions? AddOptions { get; set; }
	/// <summary>
	/// Gets or sets a value that indicates whether <see cref="Path"/> is the series' own folder.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if <see cref="Path"/> is the full path of the series' folder; <see langword="false"/> if
	/// it's the root folder that Sonarr creates the series' folder in.
	/// </value>
	public bool IsFullPath { get; set; }
	/// <summary>
	/// Gets or sets a value that indicates whether Sonarr monitors the series.
	/// </summary>
	/// <value>The value of the <c>IsMonitored</c> property, or <see langword="false"/> if it has none.</value>
	public bool IsMonitored
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}
	/// <summary>
	/// Sets the ID of the profile that the series is added with.
	/// </summary>
	/// <value>The value to write to the <c>ProfileId</c> property.</value>
	public int ProfileId
	{
		set => this.SetValue(value);
	}
	/// <summary>
	/// Gets or sets the folder that the series is added in.
	/// </summary>
	/// <value>
	/// The series' folder or its root folder, as <see cref="IsFullPath"/> says, or an empty string if none was set.
	/// </value>
	public string Path
	{
		get => _path ??= string.Empty;
		set => _path = value;
	}
	/// <summary>
	/// Gets or sets the series' type.
	/// </summary>
	/// <value>The value of the <c>SeriesType</c> property, such as <c>Standard</c>, or an empty string if it has none.</value>
	public string SeriesType
	{
		get => this.GetStringOrEmpty();
		set => this.SetValue(value);
	}
	/// <summary>
	/// Gets or sets a value that indicates whether the series' episodes are kept in season folders.
	/// </summary>
	/// <value>The value of the <c>UseSeasonFolders</c> property, or <see langword="false"/> if it has none.</value>
	public bool UseSeasonFolders
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	/// <summary>
	/// Initializes a new <see cref="AddSeriesObject"/> instance with no properties.
	/// </summary>
	public AddSeriesObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Compares the lookup result with another by title, ignoring case.
	/// </summary>
	/// <param name="other">The lookup result to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this title comes first, 0 if the titles are equal, and a positive number if this title
	/// comes after the other or <paramref name="other"/> is <see langword="null"/>.
	/// </returns>
	public int CompareTo(AddSeriesObject? other)
	{
		return this.CompareTo((SeriesObject?)other);
	}
	/// <summary>
	/// Compares the lookup result with a series by title, ignoring case.
	/// </summary>
	/// <param name="other">The series to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this title comes first, 0 if the titles are equal, and a positive number if this title
	/// comes after the other or <paramref name="other"/> is <see langword="null"/>.
	/// </returns>
	public override int CompareTo(SeriesObject? other)
	{
		return StringComparer.InvariantCultureIgnoreCase.Compare(this.Title, other?.Title);
	}
	/// <summary>
	/// Gets the ID that the lookup result pipes as a series.
	/// </summary>
	/// <returns>Always <see langword="null"/>, since the series isn't in the library yet.</returns>
	private protected override int? GetSeriesId()
	{
		return null;
	}
	/// <summary>
	/// Gets the metadata tag of the series lookup endpoint.
	/// </summary>
	/// <param name="resolver">The resolver that holds the tags.</param>
	/// <param name="existing">The tag that the object already has, which the lookup result doesn't use.</param>
	/// <returns>The <see cref="Meta.SERIES_ADD"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.SERIES_ADD];
	}

	/// <summary>
	/// Removes the properties that a series not yet in the library has no value for and sets the defaults that adding
	/// it needs.
	/// </summary>
	/// <remarks>
	/// It removes <c>Id</c> and <c>Added</c>, makes <c>LanguageProfileId</c> a read-only 1, copies the series type,
	/// and turns on season folders when the lookup lists more than one season.
	/// </remarks>
	/// <param name="alreadyCalled">
	/// <see langword="true"/> if the object has been deserialized before; otherwise, <see langword="false"/>.
	/// </param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		base.OnDeserialized(alreadyCalled);
		this.Properties.RemoveMany(Constants.ID, "Added");
		this.Properties.Remove("LanguageProfileId");
		this.Properties.Add(ReadOnlyNumberProperty.Create("LanguageProfileId", 1));

		if (this.TryGetNonNullProperty(Constants.SERIES_TYPE, out string? seriesType))
		{
			this.SeriesType = seriesType;
		}

		if (this.TryGetNonNullProperty(Constants.SEASONS, out object[]? array) && array.Length > 1)
		{
			this.Properties[Constants.USE_SEASON_FOLDER].Value = true;
			this.UseSeasonFolders = true;
		}
	}
	/// <summary>
	/// Writes the add options, title, path, and first air date into the object before it's serialized.
	/// </summary>
	public override void OnSerializing()
	{
		this.UpdateProperty(x => x.AddOptions, x => x.Title);
		this.SetPath();
		base.OnSerializing();
	}
	/// <summary>
	/// Writes <see cref="Path"/> to the <c>Path</c> property when <see cref="IsFullPath"/> is <see langword="true"/>,
	/// and to the <c>RootFolderPath</c> property otherwise, and remembers which one it wrote.
	/// </summary>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Using nameof()")]
	private void SetPath()
	{
		if (this.IsFullPath)
		{
			this.UpdateProperty(x => x.Path);
			_pathProp = nameof(Path);
		}
		else
		{
			this.SetValue(this.Path, ROOT_FOLDER_PATH);
			_pathProp = ROOT_FOLDER_PATH;
		}
	}
	/// <summary>
	/// Replaces the <see cref="SeriesObject"/> type name at the start of the object's PowerShell type names with the
	/// <see cref="AddSeriesObject"/> type name.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		Debugger.Assert(() =>
		{
			return this.TypeNames.Count > 0 && this.TypeNames[0] == typeof(SeriesObject).GetName();
		});

		this.TypeNames[0] = _typeName;  // Should overwrite 'SeriesObject'.
	}
	/// <summary>
	/// Determines whether the lookup result is read-only as a property of a parent object.
	/// </summary>
	/// <param name="propertyName">The name of the parent's property that holds the lookup result.</param>
	/// <param name="parentType">The type of the parent object.</param>
	/// <returns>Always <see langword="true"/>.</returns>
	internal override bool ShouldBeReadOnly(string propertyName, Type parentType)
	{
		return true;
	}
	/// <summary>
	/// Removes the path property that <see cref="OnSerializing"/> wrote and clears <see cref="AddOptions"/> after a
	/// request that sent the series fails.
	/// </summary>
	protected override void OnReset()
	{
		if (!string.IsNullOrEmpty(_pathProp))
		{
			this.Properties.Remove(_pathProp);
			this.IsFullPath = false;
		}

		this.AddOptions = null;
		base.OnReset();
	}
}
