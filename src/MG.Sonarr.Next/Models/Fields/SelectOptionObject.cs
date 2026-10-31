using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Fields;

/// <summary>
/// Represents one choice of a select field, in its <see cref="FieldObject.SelectOptions"/>.
/// </summary>
/// <remarks>
/// Once the choice is deserialized, its <see cref="Name"/> and <see cref="Order"/> are read-only in PowerShell. A
/// choice carries no metadata tag, since it stands for no endpoint, and choices sort by <see cref="Order"/>.
/// </remarks>
[SonarrObject]
public sealed class SelectOptionObject : SonarrObject,
	IComparable<SelectOptionObject>,
	ISerializableNames<SelectOptionObject>
{
	/// <summary>
	/// The full name of the class, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(SelectOptionObject).GetName();

	/// <summary>
	/// The number of properties that an instance is sized for, about the number of fields that Sonarr sends.
	/// </summary>
	const int CAPACITY = 3;
	/// <summary>
	/// Gets a value that indicates whether tagging the choice adds no metadata tag.
	/// </summary>
	/// <value>Always <see langword="true"/>, so a choice gets no <c>MetadataTag</c> property.</value>
	protected override bool DisregardMetadataTag => true;

	/// <summary>
	/// Gets the choice's display name.
	/// </summary>
	/// <value>The value of the <c>Name</c> property, or an empty string when the choice has none.</value>
	public string Name => this.GetStringOrEmpty();
	/// <summary>
	/// Gets the choice's position among the field's choices.
	/// </summary>
	/// <value>The value of the <c>Order</c> property, or 0 when the choice has none.</value>
	public int Order => this.GetValue<int>();

	/// <summary>
	/// Initializes a new <see cref="SelectOptionObject"/> instance with no properties, for the JSON converter to fill.
	/// </summary>
	public SelectOptionObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Compares the choice with another by their <see cref="Order"/>.
	/// </summary>
	/// <param name="other">The choice to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this choice comes before <paramref name="other"/>, 0 if they have the same order, and a
	/// positive number if this choice comes after it or <paramref name="other"/> is <see langword="null"/>.
	/// </returns>
	public int CompareTo(SelectOptionObject? other)
	{
		return Comparer<int?>.Default.Compare(this.Order, other?.Order);
	}
	/// <summary>
	/// Gets the metadata tag for a choice, which stands for no endpoint.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags, which the method ignores.</param>
	/// <param name="existing">The object's current tag.</param>
	/// <returns><paramref name="existing"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return existing;
	}

	/// <summary>
	/// Finishes the choice after it's deserialized, replacing its <c>Name</c> and <c>Order</c> properties with
	/// read-only ones.
	/// </summary>
	/// <param name="alreadyCalled"><see langword="true"/> if the object was deserialized before; otherwise,
	/// <see langword="false"/>.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in nameof()")]
	protected override void OnDeserialized(bool alreadyCalled)
	{
		this.ReplaceWithReadOnlyStringProperty(nameof(Name));
		this.ReplaceWithReadOnlyNumberProperty<int>(nameof(Order));
	}

	/// <summary>
	/// Inserts the full name of <see cref="SelectOptionObject"/> at the start of the object's type names, ahead of the
	/// base class's name.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}
}

