using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.Qualities;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.PSProperties;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.ManualImports;

/// <summary>
/// Represents a file that Sonarr found for a manual import.
/// </summary>
/// <remarks>
/// <para>
/// The object carries the <see cref="Meta.MANUAL_IMPORT"/> tag. Once it's deserialized, <see cref="Name"/> is
/// read-only in PowerShell, and the object has every property that an import needs, added when Sonarr leaves it out:
/// an <c>Episodes</c> set, a writable <c>Quality</c> and <c>Series</c>, a <c>ReleaseGroup</c>, and a
/// <c>SeasonNumber</c> script property.
/// </para>
/// <para>
/// The object isn't posted itself. <see cref="ManualImportPostMapper.ToManualImportFiles"/> turns a list of them into
/// the bodies that an import posts. Files sort by <see cref="Name"/>, ignoring case.
/// </para>
/// </remarks>
[SonarrObject]
public sealed class ManualImportObject : IdSonarrObject<ManualImportObject>,
	ISerializableNames<ManualImportObject>
{
	/// <summary>
	/// The number of properties that an instance is sized for, about the number of fields that Sonarr sends.
	/// </summary>
	const int CAPACITY = 13;
	/// <summary>
	/// The name of the property that holds the file's episodes.
	/// </summary>
	const string EPISODES = "Episodes";
	/// <summary>
	/// The name of the property that holds the file's quality.
	/// </summary>
	const string QUALITY = "Quality";
	/// <summary>
	/// The name of the script property that gives the season of the file's first episode.
	/// </summary>
	const string SEASON_NUMBER = "SeasonNumber";
	/// <summary>
	/// The name of the property that holds the file's release group.
	/// </summary>
	const string RELEASE_GROUP = "ReleaseGroup";
	/// <summary>
	/// The name of the property that holds the file's series.
	/// </summary>
	const string SERIES = "Series";

	/// <summary>
	/// The full name of the class, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(ManualImportObject).GetName();

	/// <summary>
	/// Gets the episodes that the file is imported as.
	/// </summary>
	/// <remarks>
	/// The set is the one that the <c>Episodes</c> property holds, so an episode added to it in C# shows in
	/// PowerShell and in the import's body.
	/// </remarks>
	/// <value>The file's episodes, which is empty when Sonarr matched none.</value>
	public SortedSet<EpisodeObject> Episodes { get; private set; } = null!;
	/// <summary>
	/// Gets the file's name.
	/// </summary>
	/// <value>The value of the <c>Name</c> property, or an empty string when the object has none.</value>
	public string Name => this.GetStringOrEmpty();
	/// <summary>
	/// Gets or sets the quality and revision that the file is imported with.
	/// </summary>
	/// <value>
	/// The value of the <c>Quality</c> property, or <see langword="null"/> when it has none. Setting
	/// <see langword="null"/> leaves the property as it is.
	/// </value>
	public QualityRevisionObject? Quality
	{
		get => this.GetValue<QualityRevisionObject>();
		set
		{
			if (value is not null)
			{
				this.SetValue(value);
			}
		}
	}
	/// <summary>
	/// Gets or sets the series that the file is imported into.
	/// </summary>
	/// <value>
	/// The value of the <c>Series</c> property, or <see langword="null"/> when it has none. Setting
	/// <see langword="null"/> leaves the property as it is.
	/// </value>
	public SeriesObject? Series
	{
		get => this.GetValue<SeriesObject>();
		set
		{
			if (value is not null)
			{
				this.SetValue(value);
			}
		}
	}

	/// <summary>
	/// Initializes a new <see cref="ManualImportObject"/> instance with no properties, for the JSON converter to fill.
	/// </summary>
	public ManualImportObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Compares the file with another by their <see cref="Name"/>, ignoring case.
	/// </summary>
	/// <param name="other">The file to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this file's name comes before that of <paramref name="other"/>, 0 if the names are equal,
	/// and a positive number if it comes after or <paramref name="other"/> is <see langword="null"/>.
	/// </returns>
	public override int CompareTo(ManualImportObject? other)
	{
		return StringComparer.InvariantCultureIgnoreCase.Compare(this.Name, other?.Name);
	}

	/// <summary>
	/// Gets the metadata tag for a manual import.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which the method ignores.</param>
	/// <returns>The <see cref="Meta.MANUAL_IMPORT"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.MANUAL_IMPORT];
	}

	/// <summary>
	/// Determines whether the file has what an import needs.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if the file has at least one episode, a series with an ID above 0, and a quality whose
	/// ID is above 0; otherwise, <see langword="false"/>.
	/// </returns>
	public bool IsReadyToPost()
	{
		return this.Episodes.Count > 0
			&& this.Series is SeriesObject sObj
			&& sObj.Id > 0
			&& this.Quality is QualityRevisionObject qRev
			&& qRev.Quality.Id > 0;
	}

	/// <summary>
	/// Finishes the file after it's deserialized, making its <c>Name</c> read-only and adding the properties that an
	/// import needs when Sonarr left them out.
	/// </summary>
	/// <param name="alreadyCalled"><see langword="true"/> if the object was deserialized before; otherwise,
	/// <see langword="false"/>.</param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		base.OnDeserialized(alreadyCalled);

		this.StoreAndReplaceName();
		this.AddMissingProperties();
	}

	/// <summary>
	/// Replaces the <c>Name</c> property with a read-only one that holds the same name.
	/// </summary>
	private void StoreAndReplaceName()
	{
		string name = nameof(this.Name);
		PSPropertyInfo? nameProp = this.Properties[name];
		this.Properties.Remove(name);
		this.Properties.Add(new ReadOnlyStringProperty(name, nameProp?.Value as string));
	}

	/// <summary>
	/// Adds each property that an import needs and Sonarr left out, and keeps the episode set in
	/// <see cref="Episodes"/>.
	/// </summary>
	/// <remarks>
	/// The properties added are an empty, read-only <c>Episodes</c> set, a writable <c>Quality</c> and
	/// <c>Series</c> with no value, an empty <c>ReleaseGroup</c>, and a <c>SeasonNumber</c> script property that
	/// gives the season of the first episode, or nothing when the file has none.
	/// </remarks>
	private void AddMissingProperties()
	{
		var setProp = CreateIfMissing(this.Properties, EPISODES, static (name) =>
		{
			return new ReadOnlyCollectionProperty<EpisodeObject, SortedSet<EpisodeObject>>(EPISODES, []);
		});

		this.Episodes = (SortedSet<EpisodeObject>)setProp.Value;

		CreateIfMissing(this.Properties, QUALITY, (name) =>
		{
			return new WritableSonarrProperty<QualityRevisionObject>(QUALITY);
		});
		CreateIfMissing(this.Properties, RELEASE_GROUP, static (name) => new StringNoteProperty(name, string.Empty));
		CreateIfMissing(this.Properties, SERIES, static (name) => new WritableSonarrProperty<SeriesObject>(name));
		CreateIfMissing(this.Properties, SEASON_NUMBER, static (name) =>
		{
			return new PSScriptProperty(name,
				ScriptBlock.Create("if (0 -lt $this.Episodes.Count) { $this.Episodes[0].SeasonNumber }"));
		});
	}

	/// <summary>
	/// Gets the member with the specified name from a collection, adding one when the collection has none.
	/// </summary>
	/// <typeparam name="T">The type of the collection's members.</typeparam>
	/// <param name="collection">The collection to look in and add to.</param>
	/// <param name="propertyName">The name of the member.</param>
	/// <param name="createOnMissing">The function that creates the member from <paramref name="propertyName"/> when
	/// the collection has none by that name.</param>
	/// <returns>The member that the collection already held, or the one that <paramref name="createOnMissing"/>
	/// created and the method added.</returns>
	private static T CreateIfMissing<T>(PSMemberInfoCollection<T> collection, string propertyName, Func<string, T> createOnMissing) where T : PSMemberInfo
	{
		T? prop = collection[propertyName];
		if (prop is null)
		{
			prop = createOnMissing(propertyName);
			collection.Add(prop);
		}

		return prop;
	}

	/// <summary>
	/// Inserts the full name of <see cref="ManualImportObject"/> at the start of the object's type names, ahead of the
	/// base class's name.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}
}

