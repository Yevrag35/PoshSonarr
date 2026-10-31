using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Notifications;

/// <summary>
/// Represents a template from <c>/notification/schema</c> that a new notification connection is created from.
/// </summary>
/// <remarks>
/// The object carries the <see cref="Meta.NOTIFICATION"/> tag. A new connection has no ID, so the template's <c>Id</c>
/// property is removed once it's deserialized and again before it's serialized.
/// </remarks>
[SonarrObject]
public sealed class NotificationSchemaObject : TagUpdateObject<NotificationSchemaObject>,
	IJsonOnSerializing,
	ISchemaObject,
	ISerializableNames<NotificationSchemaObject>
{
	/// <summary>
	/// The number of properties that an instance is sized for, about the number of fields that Sonarr sends.
	/// </summary>
	const int CAPACITY = 38;
	/// <summary>
	/// The full name of the class, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(NotificationSchemaObject).GetName();
	/// <summary>
	/// Gets a value that indicates whether a connection created from the template can carry tags.
	/// </summary>
	/// <value>Always <see langword="true"/>.</value>
	public bool IsTaggable => true;

	/// <summary>
	/// Initializes a new <see cref="NotificationSchemaObject"/> instance with no properties, for the JSON converter to
	/// fill.
	/// </summary>
	public NotificationSchemaObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Gets the metadata tag for a notification template, which is that of a notification connection.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which the method ignores.</param>
	/// <returns>The <see cref="Meta.NOTIFICATION"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.NOTIFICATION];
	}
	/// <summary>
	/// Finishes the template after it's deserialized, removing its <c>Id</c> property.
	/// </summary>
	/// <param name="alreadyCalled"><see langword="true"/> if the object was deserialized before; otherwise,
	/// <see langword="false"/>.</param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		base.OnDeserialized(alreadyCalled);
		this.Properties.Remove(nameof(this.Id));
	}
	/// <summary>
	/// Removes the <c>Id</c> property before the template is serialized, so the request body for the new connection
	/// carries no ID.
	/// </summary>
	public void OnSerializing()
	{
		this.Properties.Remove(nameof(this.Id));
	}
	/// <summary>
	/// Inserts the full name of <see cref="NotificationSchemaObject"/> at the start of the object's type names, ahead
	/// of the base class's name.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}
}
