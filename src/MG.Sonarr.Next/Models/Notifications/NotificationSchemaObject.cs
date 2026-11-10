using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Notifications;

/// <summary>
/// Represents a template from <c>/notification/schema</c> that a new notification connection is created from.
/// </summary>
/// <remarks>
/// The object carries the <see cref="Meta.Notification"/> tag. A new connection has no ID, so the template's <c>Id</c>
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
	private const int CAPACITY = 38;
	/// <summary>
	/// The name of the property that holds the template's implementation, such as <c>Webhook</c>.
	/// </summary>
	private const string IMPLEMENTATION = "Implementation";
	/// <summary>
	/// The name of the property that holds the implementation's display name, such as <c>Custom Script</c>.
	/// </summary>
	private const string IMPLEMENTATION_NAME = "ImplementationName";
	/// <summary>
	/// The full name of the class, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	private static readonly string _typeName = typeof(NotificationSchemaObject).GetName();
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
	/// Compares the template with another by its <c>ImplementationName</c>, and then by its <c>Implementation</c>,
	/// both ordinally, ignoring case.
	/// </summary>
	/// <remarks>
	/// A template has no ID to sort by, and this is the order that Sonarr lists the templates in, which a sort that
	/// isn't stable would scramble if the templates compared as equal.
	/// </remarks>
	/// <param name="other">The template to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this template sorts before <paramref name="other"/>, 0 if they sort together, and a
	/// positive number if it sorts after or <paramref name="other"/> is <see langword="null"/>.
	/// </returns>
	public override int CompareTo(NotificationSchemaObject? other)
	{
		if (other is null)
		{
			return 1;
		}

		int compare = StringComparer.OrdinalIgnoreCase.Compare(
			this.GetStringOrEmpty(IMPLEMENTATION_NAME),
			other.GetStringOrEmpty(IMPLEMENTATION_NAME));

		return compare != 0
			? compare
			: StringComparer.OrdinalIgnoreCase.Compare(
				this.GetStringOrEmpty(IMPLEMENTATION),
				other.GetStringOrEmpty(IMPLEMENTATION));
	}

	/// <summary>
	/// Gets the metadata tag for a notification template, which is that of a notification connection.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which the method ignores.</param>
	/// <returns>The <see cref="Meta.Notification"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.Notification];
	}
	/// <summary>
	/// Finishes the template after it's deserialized, removing its <c>Id</c> property.
	/// </summary>
	/// <remarks>
	/// The template doesn't exist on the server, so <see cref="TagUpdateObject{TSelf}.MustUpdateViaApi"/> is
	/// <see langword="false"/>, and its tags go out with the request that creates the connection.
	/// </remarks>
	/// <param name="alreadyCalled"><see langword="true"/> if the object was deserialized before; otherwise,
	/// <see langword="false"/>.</param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		base.OnDeserialized(alreadyCalled);
		this.MustUpdateViaApi = false;
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
