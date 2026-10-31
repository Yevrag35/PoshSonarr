using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Tags;

/// <summary>
/// Represents a Sonarr tag, from the <c>/tag</c> endpoint.
/// </summary>
/// <remarks>
/// The object copies its ID and label when it's deserialized, and its <see cref="IHasName"/> name is its label.
/// Tags sort by ID.
/// </remarks>
[SonarrObject]
public sealed class TagObject : SonarrObject,
	IComparable<TagObject>,
	IHasId,
	IHasName,
	ISerializableNames<TagObject>
{
	/// <summary>
	/// The number of properties that a new instance makes room for.
	/// </summary>
	const int CAPACITY = 3;

	/// <summary>
	/// Gets the tag's ID.
	/// </summary>
	/// <value>A copy of the <c>Id</c> property, taken when the object is deserialized, or 0 before then.</value>
	public int Id { get; private set; }
	/// <summary>
	/// Gets the tag's label.
	/// </summary>
	/// <value>
	/// A copy of the <c>Label</c> property, taken when the object is deserialized, or an empty string when Sonarr
	/// sent none.
	/// </value>
	public string Label { get; private set; } = string.Empty;
	/// <summary>
	/// Gets the tag's label as its name.
	/// </summary>
	/// <value>The <see cref="Label"/>.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	string IHasName.Name => this.Label;

	/// <summary>
	/// Initializes a new <see cref="TagObject"/> instance with no properties, which the JSON converter fills.
	/// </summary>
	public TagObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Compares this tag with another by ID.
	/// </summary>
	/// <param name="other">The tag to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this tag's <see cref="Id"/> is less than that of <paramref name="other"/>, 0 if they're
	/// equal, or a positive number if it's greater or <paramref name="other"/> is <see langword="null"/>.
	/// </returns>
	public int CompareTo(TagObject? other)
	{
		if (other is null)
		{
			return -1;
		}

		return this.Id.CompareTo(other.Id);
	}

	/// <summary>
	/// Gets the tag tag from a resolver.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which this method ignores.</param>
	/// <returns>The resolver's <see cref="Meta.TAG"/> tag.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.TAG];
	}

	/// <summary>
	/// Finishes the object after the JSON converter reads it, and copies its ID and label.
	/// </summary>
	/// <remarks>
	/// The base class's steps run first.
	/// </remarks>
	public override void OnDeserialized()
	{
		base.OnDeserialized();
		if (this.TryGetId(out int id))
		{
			this.Id = id;
		}

		if (this.TryGetProperty(Constants.LABEL, out string? label))
		{
			this.Label = label ?? string.Empty;
		}
	}

	/// <summary>
	/// The type's full name, which <see cref="SetPSTypeName"/> puts first in the PowerShell type names.
	/// </summary>
	static readonly string _typeName = typeof(TagObject).GetName();
	/// <summary>
	/// Puts the type's full name first in the object's PowerShell type names, ahead of the name that the base class
	/// adds.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}
}
