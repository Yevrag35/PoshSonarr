using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Media;

/// <summary>
/// Represents Sonarr's media management settings, the single object at <c>/config/mediamanagement</c>.
/// </summary>
/// <remarks>
/// <para>
/// The object carries the <see cref="Meta.MEDIA_MANGEMENT"/> tag. Sonarr's <c>recycleBin</c> field is the
/// <c>RecycleBinPath</c> property, both ways, and a <c>Path</c> alias shows it.
/// </para>
/// <para>
/// The object keeps its ID out of PowerShell's view: the <c>Id</c> property is removed once the object is
/// deserialized and its value kept in <see cref="Id"/>. <see cref="OnSerializing"/> removes the alias and adds the
/// <c>Id</c> property back for the request body, and <see cref="Commit"/> and <see cref="Reset"/> remove the property
/// and add the alias again.
/// </para>
/// </remarks>
[SonarrObject]
public sealed class MediaManagementObject : SonarrObject,
	IComparable<MediaManagementObject>,
	IJsonOnSerializing,
	ISerializableNames<MediaManagementObject>
{
	/// <summary>
	/// The <c>Path</c> alias of the <c>RecycleBinPath</c> property.
	/// </summary>
	static readonly PSAliasProperty _pathAlias = new("Path", "RecycleBinPath");
	/// <summary>
	/// The full name of the class, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(MediaManagementObject).GetName();
	/// <summary>
	/// The number of properties that an instance is sized for, about the number of fields that Sonarr sends.
	/// </summary>
	const int CAPACITY = 21;

	/// <summary>
	/// Gets the ID of the media management settings.
	/// </summary>
	/// <value>The ID that Sonarr sent, or 0 when it sent none.</value>
	public int Id { get; private set; }
	/// <summary>
	/// Gets or sets the path of the recycle bin that Sonarr moves deleted files to.
	/// </summary>
	/// <value>
	/// The value of the <c>RecycleBinPath</c> property, or an empty string when the object has none. Setting
	/// <see langword="null"/> stores an empty string.
	/// </value>
	public string RecycleBinPath
	{
		get => this.GetStringOrEmpty();
		set => this.SetValue(value ?? string.Empty);
	}

	/// <summary>
	/// Initializes a new <see cref="MediaManagementObject"/> instance with no properties, for the JSON converter to
	/// fill.
	/// </summary>
	public MediaManagementObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Removes the <c>Id</c> property and adds the <c>Path</c> alias back, after a request that sends the object
	/// succeeds.
	/// </summary>
	public override void Commit()
	{
		this.Reset();
	}
	/// <summary>
	/// Compares the settings with another by their <see cref="Id"/>.
	/// </summary>
	/// <param name="other">The settings to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this object's ID is lower than that of <paramref name="other"/>, 0 if they're the same,
	/// and a positive number if it's higher or <paramref name="other"/> is <see langword="null"/>.
	/// </returns>
	public int CompareTo(MediaManagementObject? other)
	{
		return Comparer<int?>.Default.Compare(this.Id, other?.Id);
	}
	/// <summary>
	/// Gets the metadata tag for the media management settings.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which the method ignores.</param>
	/// <returns>The <see cref="Meta.MEDIA_MANGEMENT"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.MEDIA_MANGEMENT];
	}
	/// <summary>
	/// Determines whether the recycle bin path is empty or ends with the server's directory separator.
	/// </summary>
	/// <param name="path">When the method returns, the value of <see cref="RecycleBinPath"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="path"/> is empty or white space, or ends with a directory separator;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	public bool IsRecycleBinPathProper(out string path)
	{
		path = this.RecycleBinPath;
		return string.IsNullOrWhiteSpace(path) || path.EndsWith('\\');
	}
	/// <summary>
	/// Finishes the settings after they're deserialized.
	/// </summary>
	/// <remarks>
	/// The method copies the <c>Id</c> property into <see cref="Id"/>, then removes the property and adds the
	/// <c>Path</c> alias, as <see cref="Reset"/> does.
	/// </remarks>
	public override void OnDeserialized()
	{
		base.OnDeserialized();
		if (this.TryGetId(out int id))
		{
			this.Id = id;
		}

		if (this.TryGetNonNullProperty(_pathAlias.ReferencedMemberName, out string? path))
		{
			this.RecycleBinPath = path;
		}

		this.Reset();
	}
	/// <summary>
	/// Removes the <c>Path</c> alias and adds an <c>Id</c> property that holds <see cref="Id"/> before the object is
	/// serialized, so the request body carries the ID.
	/// </summary>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Because of 'nameof()'")]
	public void OnSerializing()
	{
		this.Properties.Remove(_pathAlias.Name);
		this.UpdateProperty(this.Id, propertyName: nameof(Id));
	}
	/// <summary>
	/// Removes the <c>Id</c> property and adds the <c>Path</c> alias back, after a request that sends the object
	/// fails.
	/// </summary>
	public override void Reset()
	{
		base.Reset();
		this.Properties.Remove(Constants.ID);
		this.Properties.Add(_pathAlias);
	}
	/// <summary>
	/// Inserts the full name of <see cref="MediaManagementObject"/> at the start of the object's type names, ahead of
	/// the base class's name.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}

	/// <summary>
	/// The name maps that rename Sonarr's <c>RecycleBin</c> field to the <c>RecycleBinPath</c> property and back,
	/// built when they're first used.
	/// </summary>
	static readonly Lazy<JsonNameHolder> _names = new(() => JsonNameHolder
		.FromSingleNamePair("RecycleBin", "RecycleBinPath"));

	/// <summary>
	/// Gets the map from Sonarr's field names to the object's property names.
	/// </summary>
	/// <returns>A map that renames <c>RecycleBin</c> to <c>RecycleBinPath</c>.</returns>
	public static IReadOnlyDictionary<string, string> GetDeserializedNames()
	{
		return _names.Value.DeserializationNames;
	}
	/// <summary>
	/// Gets the map from the object's property names to Sonarr's field names.
	/// </summary>
	/// <returns>A map that renames <c>RecycleBinPath</c> to <c>RecycleBin</c>.</returns>
	public static IReadOnlyDictionary<string, string> GetSerializedNames()
	{
		return _names.Value.SerializationNames;
	}
}
