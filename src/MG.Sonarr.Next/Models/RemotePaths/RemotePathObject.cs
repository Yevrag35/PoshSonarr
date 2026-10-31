using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.RemotePaths;

/// <summary>
/// Represents a remote path mapping, which tells Sonarr where a download client's remote path is on the local
/// machine.
/// </summary>
/// <remarks>
/// The object carries the <see cref="Meta.REMOTE_PATH_MAPPING"/> tag. Sonarr's <c>host</c> field is the
/// <c>HostName</c> property, both ways. <see cref="HostName"/>, <see cref="LocalPath"/>, and
/// <see cref="RemotePath"/> are copies, which <see cref="Commit"/> and <see cref="Reset"/> write back to their
/// properties.
/// </remarks>
[SonarrObject]
public sealed class RemotePathObject : IdSonarrObject<RemotePathObject>,
	ISerializableNames<RemotePathObject>
{
	/// <summary>
	/// The number of properties that an instance is sized for, about the number of fields that Sonarr sends.
	/// </summary>
	const int CAPACITY = 5;
	/// <summary>
	/// The full name of the class, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(RemotePathObject).GetName();

	/// <summary>
	/// Gets the host of the download client that the mapping applies to.
	/// </summary>
	/// <value>A copy of the <c>HostName</c> property, or an empty string when the object has none.</value>
	public string HostName { get; private set; } = string.Empty;
	/// <summary>
	/// Gets the local path that the remote path maps to.
	/// </summary>
	/// <value>A copy of the <c>LocalPath</c> property, or an empty string when the object has none.</value>
	public string LocalPath { get; private set; } = string.Empty;
	/// <summary>
	/// Gets the path that the download client reports.
	/// </summary>
	/// <value>A copy of the <c>RemotePath</c> property, or an empty string when the object has none.</value>
	public string RemotePath { get; private set; } = string.Empty;

	/// <summary>
	/// Initializes a new <see cref="RemotePathObject"/> instance with no properties, for the JSON converter to fill.
	/// </summary>
	public RemotePathObject() : base(CAPACITY)
	{
	}

	/// <summary>
	/// Gets the metadata tag for a remote path mapping.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which the method ignores.</param>
	/// <returns>The <see cref="Meta.REMOTE_PATH_MAPPING"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.REMOTE_PATH_MAPPING];
	}

	/// <summary>
	/// Takes the values of a body that updated this mapping and commits them.
	/// </summary>
	/// <param name="body">The body that was sent to update the mapping.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="body"/> has no ID, or an ID other than this
	/// mapping's.</exception>
	public void ApplyFromPost(RemotePathBody body)
	{
		if (!body.Id.HasValue && this.Id != body.Id)
		{
			throw new ArgumentException("The body must represent an existing remote path and must match this object.");
		}

		this.HostName = body.Host;
		this.LocalPath = body.LocalPath;
		this.RemotePath = body.RemotePath;

		this.Commit();
	}


	/// <summary>
	/// Writes <see cref="HostName"/>, <see cref="LocalPath"/>, and <see cref="RemotePath"/> to their properties,
	/// after a request that sends the object succeeds.
	/// </summary>
	public override void Commit()
	{
		base.Commit();
		this.UpdateProperties();
	}

	/// <summary>
	/// Finishes the mapping after it's deserialized, copying the <c>HostName</c>, <c>LocalPath</c>, and
	/// <c>RemotePath</c> properties into <see cref="HostName"/>, <see cref="LocalPath"/>, and
	/// <see cref="RemotePath"/>.
	/// </summary>
	/// <param name="alreadyCalled"><see langword="true"/> if the object was deserialized before; otherwise,
	/// <see langword="false"/>.</param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		if (this.TryGetNonNullProperty(nameof(this.HostName), out string? host))
		{
			this.HostName = host;
		}

		if (this.TryGetNonNullProperty(nameof(this.LocalPath), out string? localPath))
		{
			this.LocalPath = localPath;
		}

		if (this.TryGetNonNullProperty(nameof(this.RemotePath), out string? remotePath))
		{
			this.RemotePath = remotePath;
		}
	}

	/// <summary>
	/// Writes <see cref="HostName"/>, <see cref="LocalPath"/>, and <see cref="RemotePath"/> back to their properties,
	/// after a request that sends the object fails.
	/// </summary>
	public override void Reset()
	{
		base.Reset();
		this.UpdateProperties();
	}

	/// <summary>
	/// Inserts the full name of <see cref="RemotePathObject"/> at the start of the object's type names, ahead of the
	/// base class's name.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}

	/// <summary>
	/// Writes <see cref="HostName"/>, <see cref="LocalPath"/>, and <see cref="RemotePath"/> to the properties of the
	/// same names, adding any that the object lacks.
	/// </summary>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Because of 'nameof()'")]
	private void UpdateProperties()
	{
		this.UpdateProperty(this.HostName, propertyName: nameof(HostName));
		this.UpdateProperty(this.LocalPath, propertyName: nameof(LocalPath));
		this.UpdateProperty(this.RemotePath, propertyName: nameof(RemotePath));
	}

	/// <summary>
	/// The name maps that rename Sonarr's <c>Host</c> field to the <c>HostName</c> property and back, built when
	/// they're first used.
	/// </summary>
	private static readonly Lazy<JsonNameHolder> _names = new(GetJsonNames);
	/// <summary>
	/// Builds the name maps that rename <c>Host</c> to <c>HostName</c> and back.
	/// </summary>
	/// <returns>The name maps for <see cref="_names"/>.</returns>
	private static JsonNameHolder GetJsonNames()
	{
		return JsonNameHolder.FromSingleNamePair("Host", "HostName");
	}

	/// <summary>
	/// Gets the map from Sonarr's field names to the object's property names.
	/// </summary>
	/// <returns>A map that renames <c>Host</c> to <c>HostName</c>.</returns>
	public static IReadOnlyDictionary<string, string> GetDeserializedNames()
	{
		return _names.Value.DeserializationNames;
	}
	/// <summary>
	/// Gets the map from the object's property names to Sonarr's field names.
	/// </summary>
	/// <returns>A map that renames <c>HostName</c> to <c>Host</c>.</returns>
	public static IReadOnlyDictionary<string, string> GetSerializedNames()
	{
		return _names.Value.SerializationNames;
	}
}

