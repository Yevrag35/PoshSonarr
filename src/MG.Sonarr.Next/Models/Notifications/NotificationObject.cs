using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.Notifications;

/// <summary>
/// Represents a notification connection that Sonarr sends events to.
/// </summary>
/// <remarks>
/// The object carries the <see cref="Meta.NOTIFICATION"/> tag, and pipes its ID as an <see cref="ITestPipeable"/>,
/// so the connection can be tested. Once it's deserialized, it has an <c>IsEnabled</c> property that tells whether
/// any of its <c>On...</c> triggers is on.
/// </remarks>
[SonarrObject]
public class NotificationObject : TagUpdateObject<NotificationObject>,
	ISerializableNames<NotificationObject>,
	ITestPipeable
{
	/// <summary>
	/// The names of the trigger properties, any of which being on makes the connection enabled.
	/// </summary>
	static readonly string[] _isEnabledProps = new[]
	{
		"OnGrab", "OnDownload", "OnUpgrade", "OnRename", "OnSeriesDelete", "OnEpisodeFileDelete",
		"OnEpisodeFileDeleteForUpgrade", "OnHealthIssue", "OnApplicationUpdate"
	};
	/// <summary>
	/// The number of properties that an instance is sized for, about the number of fields that Sonarr sends.
	/// </summary>
	const int CAPACITY = 38;
	/// <summary>
	/// The full name of the class, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(NotificationObject).GetName();

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
		this.MustUpdateViaApi = false;
	}

	/// <summary>
	/// Gets the metadata tag for a notification connection.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which the method ignores.</param>
	/// <returns>The <see cref="Meta.NOTIFICATION"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.NOTIFICATION];
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
		this.MustUpdateViaApi = false;
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
	/// The triggers are checked in the order of <see cref="_isEnabledProps"/>, and the first one that's on ends the
	/// check. A trigger that's missing, can't be read, or doesn't hold a <see cref="bool"/> ends it with
	/// <see langword="false"/>, as does a collection known to hold fewer properties than there are triggers.
	/// </remarks>
	/// <typeparam name="T">The type of the collection's properties.</typeparam>
	/// <param name="properties">The connection's properties.</param>
	/// <returns>
	/// <see langword="true"/> if a trigger that's on comes before any trigger that's missing or isn't a
	/// <see cref="bool"/>; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool CalculateIsEnabled<T>(PSMemberInfoCollection<T> properties) where T : PSPropertyInfo
	{
		if (properties.TryGetNonEnumeratedCount(out int count) && count < _isEnabledProps.Length)
		{
			return false;
		}

		foreach (string propName in _isEnabledProps)
		{
			if (!TryGetInfo(properties, propName, out T? info)
				||
				!info.IsGettable
				||
				info.Value is not bool boolVal)
			{
				return false;
			}
			else if (boolVal)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Gets the property with the specified name from a collection.
	/// </summary>
	/// <typeparam name="T">The type of the collection's properties.</typeparam>
	/// <param name="properties">The collection to look in.</param>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="result">When the method returns, the property, or <see langword="null"/> if the collection has
	/// none by that name.</param>
	/// <returns>
	/// <see langword="true"/> if the collection has a property named <paramref name="propertyName"/>; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	private static bool TryGetInfo<T>(PSMemberInfoCollection<T> properties, string propertyName, [NotNullWhen(true)] out T? result) where T : PSPropertyInfo
	{
		result = properties[propertyName];
		return result is not null;
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
