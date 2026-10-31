using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.PSProperties;
using System.Collections.Immutable;

namespace MG.Sonarr.Next.Models.Fields;

/// <summary>
/// Represents one setting of a provider, such as an indexer, a download client, or a notification, in its parent's
/// <c>Fields</c>.
/// </summary>
/// <remarks>
/// <para>
/// Once the field is deserialized, every property that describes it is read-only in PowerShell, and only its
/// <c>Value</c> stays writable. A field carries no metadata tag, since it stands for no endpoint.
/// </para>
/// <para>
/// Fields sort by <see cref="Order"/>.
/// </para>
/// </remarks>
[SonarrObject]
public sealed class FieldObject : SonarrObject,
	IComparable<FieldObject>,
	ISerializableNames<FieldObject>
{
	/// <summary>
	/// The number of properties that an instance is sized for, about the number of fields that Sonarr sends.
	/// </summary>
	const int CAPACITY = 8;
	/// <summary>
	/// The full name of the class, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(FieldObject).GetName();
	/// <summary>
	/// Gets a value that indicates whether tagging the field adds no metadata tag.
	/// </summary>
	/// <value>Always <see langword="true"/>, so a field gets no <c>MetadataTag</c> property.</value>
	protected override bool DisregardMetadataTag => true;
	/// <summary>
	/// Gets a value that indicates whether the field is an advanced setting.
	/// </summary>
	/// <value>The value of the <c>Advanced</c> property, or <see langword="false"/> when the field has none.</value>
	public bool Advanced => this.GetValue<bool>();
	/// <summary>
	/// Gets the help text that describes the field.
	/// </summary>
	/// <value>The value of the <c>HelpText</c> property, or an empty string when the field has none.</value>
	public string HelpText => this.GetStringOrEmpty();
	/// <summary>
	/// Gets a value that indicates whether the field's value is a floating-point number.
	/// </summary>
	/// <value>The value of the <c>IsFloat</c> property, or <see langword="false"/> when the field has none.</value>
	public bool IsFloat => this.GetValue<bool>();
	/// <summary>
	/// Gets the field's display label.
	/// </summary>
	/// <value>The value of the <c>Label</c> property, or an empty string when the field has none.</value>
	public string Label => this.GetStringOrEmpty();
	/// <summary>
	/// Gets the field's name.
	/// </summary>
	/// <value>The value of the <c>Name</c> property, or an empty string when the field has none.</value>
	public string Name => this.GetStringOrEmpty();
	/// <summary>
	/// Gets the field's position among its parent's fields.
	/// </summary>
	/// <value>The value of the <c>Order</c> property, or 0 when the field has none.</value>
	public int Order => this.GetValue<int>();
	/// <summary>
	/// Gets how private the field's value is, such as whether it holds a password or an API key.
	/// </summary>
	/// <value>The value of the <c>Privacy</c> property, or an empty string when the field has none.</value>
	public string Privacy => this.GetStringOrEmpty();
	/// <summary>
	/// Gets the choices of a select field.
	/// </summary>
	/// <value>
	/// The choices in the <c>SelectOptions</c> property, or an empty list when the field isn't a select field.
	/// </value>
	public IReadOnlyList<SelectOptionObject> SelectOptions { get; private set; }
	/// <summary>
	/// Gets the kind of input that the field takes, such as a textbox, a checkbox, or a select field.
	/// </summary>
	/// <value>The value of the <c>Type</c> property, or an empty string when the field has none.</value>
	public string Type => this.GetStringOrEmpty();

	/// <summary>
	/// Initializes a new <see cref="FieldObject"/> instance with no properties and no select options, for the JSON
	/// converter to fill.
	/// </summary>
	public FieldObject()
		: base(CAPACITY)
	{
		this.SelectOptions = ImmutableArray<SelectOptionObject>.Empty;
	}

	/// <summary>
	/// Compares the field with another by their <see cref="Order"/>.
	/// </summary>
	/// <param name="other">The field to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this field comes before <paramref name="other"/>, 0 if they have the same order, and a
	/// positive number if this field comes after it or <paramref name="other"/> is <see langword="null"/>.
	/// </returns>
	public int CompareTo(FieldObject? other)
	{
		return Comparer<int?>.Default.Compare(this.Order, other?.Order);
	}

	/// <summary>
	/// Gets the metadata tag for a field, which stands for no endpoint.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags, which the method ignores.</param>
	/// <param name="existing">The object's current tag.</param>
	/// <returns><paramref name="existing"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return existing;
	}

	/// <summary>
	/// Finishes the field after it's deserialized.
	/// </summary>
	/// <remarks>
	/// The method replaces every property that describes the field with a read-only one, which leaves <c>Value</c> as
	/// the one writable property. It keeps the <c>SelectOptions</c> list in <see cref="SelectOptions"/>, or adds an
	/// empty, read-only <c>SelectOptions</c> property when Sonarr sent none.
	/// </remarks>
	/// <param name="alreadyCalled"><see langword="true"/> if the object was deserialized before; otherwise,
	/// <see langword="false"/>.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in nameof()")]
	protected override void OnDeserialized(bool alreadyCalled)
	{
		base.OnDeserialized(alreadyCalled);

		this.ReplaceWithReadOnlyStringProperty(nameof(HelpText));
		this.ReplaceWithReadOnlyStringProperty(nameof(Label));
		this.ReplaceWithReadOnlyStringProperty(nameof(Name));
		this.ReplaceWithReadOnlyStringProperty(nameof(Privacy));
		this.ReplaceWithReadOnlyStringProperty(nameof(Type));
		this.ReplaceWithReadOnlyNumberProperty<int>(nameof(Order));
		this.ReplaceWithReadOnlyStructProperty<bool>(nameof(IsFloat));
		this.ReplaceWithReadOnlyStructProperty<bool>(nameof(Advanced));

		if (this.TryGetNonNullProperty(nameof(SelectOptions), out IReadOnlyList<SelectOptionObject>? list))
		{
			this.SelectOptions = list;
		}
		else
		{
			var array = ImmutableArray<SelectOptionObject>.Empty;
			this.Properties.Add(new ReadOnlyCollectionProperty<SelectOptionObject>(nameof(SelectOptions), array));
			this.SelectOptions = array;
		}
	}

	/// <summary>
	/// Inserts the full name of <see cref="FieldObject"/> at the start of the object's type names, ahead of the base
	/// class's name.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}
}

