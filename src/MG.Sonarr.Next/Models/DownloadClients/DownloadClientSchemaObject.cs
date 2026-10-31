using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.DownloadClients;

/// <summary>
/// Represents a template from <c>/downloadclient/schema</c> that a new download client is created from.
/// </summary>
/// <remarks>
/// The object carries the <see cref="Meta.DOWNLOAD_CLIENT_SCHEMA"/> tag. Once it's deserialized, the properties that
/// describe the client's kind, <see cref="ConfigContract"/>, <see cref="Implementation"/>,
/// <see cref="ImplementationName"/>, <see cref="InfoLink"/>, and <see cref="Protocol"/>, are read-only in PowerShell,
/// while <see cref="Enable"/>, <see cref="Name"/>, and <see cref="Priority"/> stay writable for the new client.
/// </remarks>
[SonarrObject]
public sealed class DownloadClientSchemaObject : TagUpdateObject<DownloadClientSchemaObject>,
	ISerializableNames<DownloadClientSchemaObject>
{
	/// <summary>
	/// The number of properties that an instance is sized for, about the number of fields that Sonarr sends.
	/// </summary>
	const int CAPACITY = 15;
	/// <summary>
	/// The full name of the class, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(DownloadClientSchemaObject).GetName();

	/// <summary>
	/// Gets the name of the settings contract that the client's fields follow.
	/// </summary>
	/// <value>The value of the <c>ConfigContract</c> property, or an empty string when the object has none.</value>
	public string ConfigContract
	{
		get => this.GetStringOrEmpty();
	}
	/// <summary>
	/// Gets or sets a value that indicates whether the new client is enabled.
	/// </summary>
	/// <value>The value of the <c>Enable</c> property, or <see langword="false"/> when the object has none.</value>
	public bool Enable
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}
	/// <summary>
	/// Gets the name of the implementation that the client uses.
	/// </summary>
	/// <value>The value of the <c>Implementation</c> property, or an empty string when the object has none.</value>
	public string Implementation
	{
		get => this.GetStringOrEmpty();
	}
	/// <summary>
	/// Gets the display name of the implementation that the client uses.
	/// </summary>
	/// <value>The value of the <c>ImplementationName</c> property, or an empty string when the object has none.</value>
	public string ImplementationName
	{
		get => this.GetStringOrEmpty();
	}
	/// <summary>
	/// Gets the link to the documentation of the client's kind.
	/// </summary>
	/// <value>The value of the <c>InfoLink</c> property, or an empty string when the object has none.</value>
	public string InfoLink
	{
		get => this.GetStringOrEmpty();
	}
	/// <summary>
	/// Gets or sets the new client's name.
	/// </summary>
	/// <value>The value of the <c>Name</c> property, or an empty string when the object has none.</value>
	public string Name
	{
		get => this.GetStringOrEmpty();
		set => this.SetValue(value);
	}
	/// <summary>
	/// Gets the download protocol that the client handles, such as usenet or torrent.
	/// </summary>
	/// <value>The value of the <c>Protocol</c> property, or an empty string when the object has none.</value>
	public string Protocol
	{
		get => this.GetStringOrEmpty();
	}
	/// <summary>
	/// Gets or sets the new client's priority.
	/// </summary>
	/// <value>The value of the <c>Priority</c> property, or 0 when the object has none.</value>
	public int Priority
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	/// <summary>
	/// Initializes a new <see cref="DownloadClientSchemaObject"/> instance with no properties, for the JSON converter
	/// to fill.
	/// </summary>
	public DownloadClientSchemaObject() : base(CAPACITY)
	{
	}

	/// <summary>
	/// Creates a copy of the template, so a new client can be built from it without changing the original.
	/// </summary>
	/// <remarks>
	/// Each property is copied into a new instance, which gets the type names of a
	/// <see cref="DownloadClientSchemaObject"/>.
	/// </remarks>
	/// <returns>A new <see cref="DownloadClientSchemaObject"/> with a copy of each of this object's properties.</returns>
	public override PSObject Copy()
	{
		DownloadClientSchemaObject copy = new();
		foreach (PSPropertyInfo prop in this.Properties)
		{
			copy.Members.Add(prop.Copy());
		}

		copy.SetPSTypeName();
		return copy;
	}

	/// <summary>
	/// Inserts the full name of <see cref="DownloadClientSchemaObject"/> at the start of the object's type names, ahead
	/// of the base class's name.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}

	/// <summary>
	/// Gets the metadata tag for a download client template.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which the method ignores.</param>
	/// <returns>The <see cref="Meta.DOWNLOAD_CLIENT_SCHEMA"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.DOWNLOAD_CLIENT_SCHEMA];
	}

	/// <summary>
	/// Finishes the template after it's deserialized.
	/// </summary>
	/// <remarks>
	/// After the base takes the template's tags, the method replaces <see cref="ConfigContract"/>,
	/// <see cref="Implementation"/>, <see cref="ImplementationName"/>, <see cref="InfoLink"/>, and
	/// <see cref="Protocol"/> with read-only properties.
	/// </remarks>
	/// <param name="alreadyCalled"><see langword="true"/> if the object was deserialized before; otherwise,
	/// <see langword="false"/>.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in nameof()")]
	protected override void OnDeserialized(bool alreadyCalled)
	{
		base.OnDeserialized(alreadyCalled);

		this.ReplaceWithReadOnlyStringProperty(nameof(ConfigContract));
		this.ReplaceWithReadOnlyStringProperty(nameof(Implementation));
		this.ReplaceWithReadOnlyStringProperty(nameof(ImplementationName));
		this.ReplaceWithReadOnlyStringProperty(nameof(InfoLink));
		this.ReplaceWithReadOnlyStringProperty(nameof(Protocol));
	}
}