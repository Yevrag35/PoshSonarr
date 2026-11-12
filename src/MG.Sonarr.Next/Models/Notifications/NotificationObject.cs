using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.Pwsh;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Notifications;

/// <summary>
/// Represents a notification connection that Sonarr sends events to.
/// </summary>
/// <remarks>
/// The object carries the <see cref="Meta.Notification"/> tag, and pipes its ID as an <see cref="ITestPipeable"/>,
/// so the connection can be tested. Once it's deserialized, it has an <c>IsEnabled</c> property that tells whether
/// any of its <c>On...</c> triggers is on.
/// </remarks>
[SonarrObject]
public class NotificationObject : TagUpdateObject<NotificationObject>,
	ISerializableNames<NotificationObject>,
	ITestPipeable
{
	/// <summary>
	/// The text that starts the name of each trigger property, such as <c>OnGrab</c>, any of which being on makes the
	/// connection enabled.
	/// </summary>
	private const string TRIGGER_PREFIX = "On";
	/// <summary>
	/// The number of properties that an instance is sized for, about the number of fields that Sonarr sends.
	/// </summary>
	private const int CAPACITY = 38;
	/// <summary>
	/// The full name of the class, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	private static readonly string _typeName = typeof(NotificationObject).GetName();

	/// <summary>
	/// Gets or sets the connection's name.
	/// </summary>
	/// <remarks>
	/// The property reads and writes the <c>Name</c> property, so it follows a change that PowerShell makes.
	/// </remarks>
	/// <value>The value of the <c>Name</c> property, or an empty string when the object has none.</value>
	public string Name
	{
		get => this.GetStringOrEmpty();
		set => this.SetValue(value);
	}

	/// <summary>
	/// Initializes a new <see cref="NotificationObject"/> instance with no properties, for the JSON converter to fill.
	/// </summary>
	public NotificationObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Gets the metadata tag for a notification connection.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which the method ignores.</param>
	/// <returns>The <see cref="Meta.Notification"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.Notification];
	}

	/// <summary>
	/// Finishes the connection after it's deserialized, adding an <c>IsEnabled</c> property.
	/// </summary>
	/// <remarks>
	/// The connection exists on the server, so <see cref="TagUpdateObject{TSelf}.MustUpdateViaApi"/> is
	/// <see langword="true"/>, as the base sets it, and a change to its tags is sent in a PUT.
	/// </remarks>
	/// <param name="alreadyCalled"><see langword="true"/> if the object was deserialized before; otherwise,
	/// <see langword="false"/>.</param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		base.OnDeserialized(alreadyCalled);
		bool isEnabled = CalculateIsEnabled(this.Properties);
		this.Properties.Add(new PSNoteProperty("IsEnabled", isEnabled));
	}

	/// <summary>
	/// Inserts the full name of <see cref="NotificationObject"/> at the start of the object's type names, ahead of
	/// the base class's name.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}

	/// <summary>
	/// Determines whether any of the connection's triggers is on.
	/// </summary>
	/// <remarks>
	/// A trigger is a property whose name is <c>On</c> followed by an uppercase letter, such as <c>OnGrab</c> or
	/// <c>OnSeriesAdd</c>, so the triggers that a later Sonarr adds count too. A trigger that doesn't hold a
	/// <see cref="bool"/> counts as off.
	/// </remarks>
	/// <typeparam name="T">The type of the collection's properties.</typeparam>
	/// <param name="properties">The connection's properties.</param>
	/// <returns>
	/// <see langword="true"/> if a trigger holds <see langword="true"/>; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool CalculateIsEnabled<T>(PSMemberInfoCollection<T> properties) where T : PSPropertyInfo
	{
		foreach (T property in properties)
		{
			if (IsTriggerName(property.Name) && property.IsGettable && property.Value is true)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Determines whether a property's name is a trigger's.
	/// </summary>
	/// <param name="name">The property's name.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="name"/> is <c>On</c> followed by an uppercase ASCII letter and
	/// anything after it; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool IsTriggerName(string name)
	{
		return name.Length > TRIGGER_PREFIX.Length
			&& name.StartsWith(TRIGGER_PREFIX, StringComparison.Ordinal)
			&& char.IsAsciiLetterUpper(name[TRIGGER_PREFIX.Length]);
	}

	/// <summary>
	/// Gets the ID of the connection to test.
	/// </summary>
	/// <returns>The connection's <see cref="IdSonarrObject{TSelf}.Id"/>.</returns>
	public int? GetId()
	{
		return this.Id;
	}
}
