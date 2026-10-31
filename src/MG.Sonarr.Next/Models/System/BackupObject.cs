using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.System;

/// <summary>
/// Represents a backup of the Sonarr server, from <c>/system/backup</c>.
/// </summary>
/// <remarks>
/// <para>
/// Sonarr's <c>path</c> field reads as the <c>BackupUri</c> property and is written back as <c>path</c>.
/// </para>
/// <para>
/// Backups sort by name, ignoring case, and then by ID.
/// </para>
/// </remarks>
[SonarrObject]
public sealed class BackupObject : IdSonarrObject<BackupObject>,
	ISerializableNames<BackupObject>
{
	/// <summary>
	/// The number of properties that a new backup reserves room for.
	/// </summary>
	const int CAPACITY = 6;
	/// <summary>
	/// The full name of the type, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(BackupObject).GetName();

	/// <summary>
	/// Gets the backup's path, relative to the server.
	/// </summary>
	/// <remarks>
	/// It's a copy of the <c>BackupUri</c> property, taken while the backup is deserialized, and stays
	/// <see langword="null"/> when that property is missing or isn't a relative URI.
	/// </remarks>
	/// <value>The relative URI of the backup file.</value>
	public Uri BackupUri { get; private set; } = null!;
	/// <summary>
	/// Gets the backup's file name.
	/// </summary>
	/// <remarks>
	/// It's a copy of the <c>Name</c> property, taken while the backup is deserialized.
	/// </remarks>
	/// <value>The name of the backup file, or an empty string when Sonarr sends none.</value>
	public string Name { get; private set; } = string.Empty;

	/// <summary>
	/// Initializes a new <see cref="BackupObject"/> instance with no properties.
	/// </summary>
	public BackupObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Compares this backup with another by name and then by ID.
	/// </summary>
	/// <param name="other">The backup to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this backup sorts before <paramref name="other"/>, 0 if they sort together, or a positive
	/// number if it sorts after. Names compare by the invariant culture, ignoring case.
	/// </returns>
	public override int CompareTo(BackupObject? other)
	{
		int compare = StringComparer.InvariantCultureIgnoreCase.Compare(this.Name, other?.Name);
		if (compare == 0)
		{
			compare = base.CompareTo(other);
		}

		return compare;
	}

	/// <summary>
	/// Gets the metadata tag for backups.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's tags.</param>
	/// <param name="existing">The tag that the object has now, which a backup doesn't use.</param>
	/// <returns>The resolver's tag for the backup endpoint.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.BACKUP];
	}

	/// <summary>
	/// Copies the backup's name and path into <see cref="Name"/> and <see cref="BackupUri"/>.
	/// </summary>
	/// <param name="alreadyCalled">
	/// <see langword="true"/> if the object has been deserialized before; otherwise, <see langword="false"/>.
	/// </param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		base.OnDeserialized(alreadyCalled);

		if (this.TryGetNonNullProperty(nameof(this.Name), out string? name))
		{
			this.Name = name;
		}

		if (this.TryGetNonNullProperty(nameof(this.BackupUri), out string? possible)
			&&
			Uri.TryCreate(possible, UriKind.Relative, out Uri? backupUri))
		{
			this.BackupUri = backupUri;
		}
	}
	/// <summary>
	/// Inserts the backup's own type name ahead of the base type names.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}

	/// <summary>
	/// The backup's name maps, built the first time they're read.
	/// </summary>
	private static readonly Lazy<JsonNameHolder> _names = new(GetJsonNames);

	/// <summary>
	/// Builds the name maps that rename <c>Path</c> to <c>BackupUri</c> and back.
	/// </summary>
	/// <returns>The name maps for the deserialized and serialized names.</returns>
	private static JsonNameHolder GetJsonNames()
	{
		return JsonNameHolder.FromSingleNamePair("Path", "BackupUri");
	}

	/// <summary>
	/// Gets the map from the JSON fields' names to the names of the properties that they read into.
	/// </summary>
	/// <returns>A map that renames <c>Path</c> to <c>BackupUri</c>.</returns>
	public static IReadOnlyDictionary<string, string> GetDeserializedNames()
	{
		return _names.Value.DeserializationNames;
	}
	/// <summary>
	/// Gets the map from the properties' names to the names of the JSON fields that they're written as.
	/// </summary>
	/// <returns>A map that renames <c>BackupUri</c> to <c>Path</c>.</returns>
	public static IReadOnlyDictionary<string, string> GetSerializedNames()
	{
		return _names.Value.SerializationNames;
	}
}
