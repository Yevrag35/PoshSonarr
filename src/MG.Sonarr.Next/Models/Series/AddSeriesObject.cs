using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.Pwsh;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.PSProperties;

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
/// <c>RootFolderPath</c> otherwise. It first undoes the path that an earlier call wrote when no commit or reset
/// followed it, so the body holds only the path property that <see cref="IsFullPath"/> names, however often the
/// object is serialized. A commit or a reset takes them out again: it removes the path and <c>AddOptions</c>
/// properties, or gives a path property that the object already had its earlier value back, and clears
/// <see cref="AddOptions"/>.
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
	private const int CAPACITY = 50;
	/// <summary>
	/// The name of the property that holds the series' own folder.
	/// </summary>
	private const string PATH = nameof(Path);
	/// <summary>
	/// The name of the property that holds the root folder that the series is added under.
	/// </summary>
	private const string ROOT_FOLDER_PATH = "RootFolderPath";
	/// <summary>
	/// The PowerShell type name of <see cref="AddSeriesObject"/>.
	/// </summary>
	private static readonly string s_typeName = typeof(AddSeriesObject).GetName();
	/// <summary>
	/// The path that <see cref="Path"/> returns.
	/// </summary>
	private string? _path;
	/// <summary>
	/// The path property that <see cref="SetPath"/> wrote and what it held before, which <see cref="RestorePath"/>
	/// puts back, or the default value when no path property is written.
	/// </summary>
	private PathWrite _pathWrite;

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
		return StringComparer.OrdinalIgnoreCase.Compare(this.Title, other?.Title);
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
	/// <returns>The <see cref="Meta.SeriesAdd"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.SeriesAdd];
	}

	/// <summary>
	/// Removes the properties that a series not yet in the library has no value for and sets the defaults that adding
	/// it needs.
	/// </summary>
	/// <remarks>
	/// It removes <c>Id</c> and <c>Added</c>, makes <c>LanguageProfileId</c> a read-only 1, copies the series type,
	/// and turns on season folders when the lookup lists more than one season. The series doesn't exist on the
	/// server until it's added, so <see cref="TagUpdateObject{TSelf}.MustUpdateViaApi"/> is
	/// <see langword="false"/>, and its tags go out with the request that adds it.
	/// </remarks>
	/// <param name="alreadyCalled">
	/// <see langword="true"/> if the object has been deserialized before; otherwise, <see langword="false"/>.
	/// </param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		base.OnDeserialized(alreadyCalled);
		this.MustUpdateViaApi = false;
		this.Properties.RemoveMany(Constants.Id, "Added");
		this.Properties.Remove("LanguageProfileId");
		this.Properties.Add(ReadOnlyNumberProperty.Create("LanguageProfileId", 1));

		if (this.TryGetNonNullProperty(Constants.SeriesType, out string? seriesType))
		{
			this.SeriesType = seriesType;
		}

		if (this.TryGetNonNullProperty(Constants.Seasons, out object[]? array) && array.Length > 1)
		{
			this.Properties[Constants.UseSeasonFolder].Value = true;
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
	/// and to the <c>RootFolderPath</c> property otherwise, and remembers which one it wrote and what that property
	/// held before.
	/// </summary>
	/// <remarks>
	/// <para>
	/// It first undoes the path that an earlier call wrote, through <see cref="RestorePath"/>, so it always starts from
	/// the object as PowerShell shows it. Serializing the object again before a commit or reset, as writing its body to
	/// the debug stream and then sending it does, or as an add does after an earlier one stopped between the two, then
	/// leaves only the property that <see cref="IsFullPath"/> names, and what the property held before the first write
	/// is what a commit or reset puts back.
	/// </para>
	/// <para>
	/// The write is remembered only once it's made, so a write that throws leaves nothing for
	/// <see cref="RestorePath"/> to undo.
	/// </para>
	/// </remarks>
	private void SetPath()
	{
		this.RestorePath();

		PathProperty property = this.IsFullPath ? PathProperty.Path : PathProperty.RootFolderPath;
		string name = GetPathName(property);
		PathWrite write = new(property, this.Properties[name]);
		this.UpdateProperty(this.Path, propertyName: name);
		_pathWrite = write;
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

		this.TypeNames[0] = s_typeName;  // Should overwrite 'SeriesObject'.
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
	/// Takes out what <see cref="OnSerializing"/> wrote, after a request that sent the series succeeds.
	/// </summary>
	/// <inheritdoc cref="RemoveRequestProperties" path="/remarks"/>
	protected override void OnCommit()
	{
		this.RemoveRequestProperties();
		base.OnCommit();
	}
	/// <summary>
	/// Takes out what <see cref="OnSerializing"/> wrote, after a request that sent the series fails.
	/// </summary>
	/// <inheritdoc cref="RemoveRequestProperties" path="/remarks"/>
	protected override void OnReset()
	{
		this.RemoveRequestProperties();
		base.OnReset();
	}
	/// <summary>
	/// Takes out the properties that <see cref="OnSerializing"/> wrote for the request that adds the series.
	/// </summary>
	/// <remarks>
	/// The <c>AddOptions</c> property is removed and <see cref="AddOptions"/> cleared. The path property is removed, or,
	/// when the object already had that property before the path was written to it, the property gets back the value
	/// that it held, and <see cref="IsFullPath"/> is set to <see langword="false"/>.
	/// </remarks>
	private void RemoveRequestProperties()
	{
		if (this.RestorePath())
		{
			this.IsFullPath = false;
		}

		this.Properties.Remove(nameof(this.AddOptions));
		this.AddOptions = null;
	}
	/// <summary>
	/// Undoes the path that <see cref="SetPath"/> wrote: gives the property back the value that it held before, or
	/// removes it when the object didn't have it.
	/// </summary>
	/// <remarks>
	/// The write is forgotten before it's undone, so an undo that throws isn't tried again by every later call.
	/// </remarks>
	/// <returns>
	/// <see langword="true"/> if a path was written and is now undone; <see langword="false"/> if none was written.
	/// </returns>
	private bool RestorePath()
	{
		PathWrite write = _pathWrite;
		if (write.Property == PathProperty.None)
		{
			return false;
		}

		_pathWrite = default;
		string name = GetPathName(write.Property);
		if (write.Existed)
		{
			this.UpdateProperty(write.Before, propertyName: name);
		}
		else
		{
			this.Properties.Remove(name);
		}

		return true;
	}

	/// <summary>
	/// Gets the name of the property that the specified path property stands for.
	/// </summary>
	/// <param name="property">The path property.</param>
	/// <returns><c>Path</c> or <c>RootFolderPath</c>.</returns>
	/// <exception cref="UnreachableException">
	/// Thrown when <paramref name="property"/> is <see cref="PathProperty.None"/> or isn't a defined value.
	/// </exception>
	private static string GetPathName(PathProperty property)
	{
		return property switch
		{
			PathProperty.Path => PATH,
			PathProperty.RootFolderPath => ROOT_FOLDER_PATH,
			_ => throw new UnreachableException(),
		};
	}

	/// <summary>
	/// Specifies the property that <see cref="SetPath"/> wrote the path to.
	/// </summary>
	private enum PathProperty : byte
	{
		/// <summary>
		/// No path property is written.
		/// </summary>
		None,
		/// <summary>
		/// The <c>RootFolderPath</c> property, for the root folder that Sonarr creates the series' folder in.
		/// </summary>
		RootFolderPath,
		/// <summary>
		/// The <c>Path</c> property, for the series' own folder.
		/// </summary>
		Path,
	}

	/// <summary>
	/// Represents a write of the path to a path property, with what the property held before the write.
	/// </summary>
	/// <remarks>
	/// A default instance stands for no write: its <see cref="Property"/> is <see cref="PathProperty.None"/>.
	/// </remarks>
	private readonly struct PathWrite
	{
		private readonly object? _before;
		private readonly PathProperty _property;
		private readonly bool _existed;

		/// <summary>
		/// Gets the value that the property held before the write.
		/// </summary>
		/// <value>The property's earlier value, or <see langword="null"/> when the object didn't have the property.</value>
		internal object? Before => _before;
		/// <summary>
		/// Gets a value that indicates whether the object had the property before the write.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the object had the property before the write; otherwise, <see langword="false"/>.
		/// </value>
		internal bool Existed => _existed;
		/// <summary>
		/// Gets the property that the path was written to.
		/// </summary>
		/// <value>The path property, or <see cref="PathProperty.None"/> for a default instance.</value>
		internal PathProperty Property => _property;

		/// <summary>
		/// Initializes a new <see cref="PathWrite"/> instance for a write to the specified property, from the property as
		/// it was before the write.
		/// </summary>
		/// <param name="property">The path property that the path is written to.</param>
		/// <param name="before">The property before the write, or <see langword="null"/> when the object doesn't have it.</param>
		internal PathWrite(PathProperty property, PSPropertyInfo? before)
		{
			_property = property;
			_existed = before is not null;
			_before = before?.Value;
		}
	}
}
