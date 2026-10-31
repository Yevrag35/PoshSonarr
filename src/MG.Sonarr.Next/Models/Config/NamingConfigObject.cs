using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Config;

/// <summary>
/// Represents Sonarr's episode naming settings, the single object at <c>/config/naming</c>.
/// </summary>
/// <remarks>
/// <para>
/// The object carries the <see cref="Meta.NAMING_CONFIG"/> tag. It keeps its ID out of PowerShell's view: the
/// <c>Id</c> property is removed once the object is deserialized and its value kept in <see cref="Id"/>,
/// <see cref="OnSerializing"/> adds the property back for the request body, and <see cref="Commit"/> and
/// <see cref="Reset"/> remove it again.
/// </para>
/// </remarks>
[SonarrObject]
public sealed class NamingConfigObject : SonarrObject,
	IHasId,
	IJsonOnSerializing,
	ISerializableNames<NamingConfigObject>
{
	/// <summary>
	/// The number of properties that an instance is sized for, about the number of fields that Sonarr sends.
	/// </summary>
	const int CAPACITY = 16;
	/// <summary>
	/// The full name of the class, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(NamingConfigObject).GetName();

	/// <summary>
	/// Gets the ID of the naming settings.
	/// </summary>
	/// <value>The ID that Sonarr sent, or 0 when it sent none.</value>
	public int Id { get; private set; }

	/// <summary>
	/// Initializes a new <see cref="NamingConfigObject"/> instance with no properties, for the JSON converter to fill.
	/// </summary>
	public NamingConfigObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Removes the <c>Id</c> property that <see cref="OnSerializing"/> added, after a request that sends the object
	/// succeeds.
	/// </summary>
	public override void Commit()
	{
		base.Commit();
		this.Properties.Remove(nameof(this.Id));
	}
	/// <summary>
	/// Gets the metadata tag for the naming settings.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which the method ignores.</param>
	/// <returns>The <see cref="Meta.NAMING_CONFIG"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.NAMING_CONFIG];
	}

	/// <summary>
	/// Finishes the object after it's deserialized, copying the <c>Id</c> property into <see cref="Id"/> and then
	/// removing the property.
	/// </summary>
	public override void OnDeserialized()
	{
		base.OnDeserialized();
		if (this.TryGetId(out int id))
		{
			this.Id = id;
		}

		this.Properties.Remove(nameof(this.Id));
	}
	/// <summary>
	/// Adds an <c>Id</c> property that holds <see cref="Id"/> before the object is serialized, so the request body
	/// carries it.
	/// </summary>
	public void OnSerializing()
	{
		this.ReplaceNumberProperty(nameof(this.Id), this.Id);
	}
	/// <summary>
	/// Removes the <c>Id</c> property that <see cref="OnSerializing"/> added, after a request that sends the object
	/// fails.
	/// </summary>
	public override void Reset()
	{
		base.Reset();
		this.Properties.Remove(nameof(this.Id));
	}
	/// <summary>
	/// Inserts the full name of <see cref="NamingConfigObject"/> at the start of the object's type names, ahead of
	/// the base class's name.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}
}

