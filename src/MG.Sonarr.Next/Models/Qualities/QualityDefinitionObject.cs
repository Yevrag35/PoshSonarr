using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Qualities;

/// <summary>
/// Represents the size limits of one quality, from the <c>/qualitydefinition</c> endpoint.
/// </summary>
/// <remarks>
/// The definition's nested <c>Quality</c> is a <see cref="QualityObject"/>. Definitions sort by ID.
/// </remarks>
[SonarrObject]
public sealed class QualityDefinitionObject : SonarrObject,
	IComparable<QualityDefinitionObject>,
	ISerializableNames<QualityDefinitionObject>
{
	/// <summary>
	/// The number of properties that a new instance makes room for.
	/// </summary>
	const int CAPACITY = 10;
	/// <summary>
	/// The type's full name, which <see cref="SetPSTypeName"/> puts first in the PowerShell type names.
	/// </summary>
	static readonly string _typeName = typeof(QualityDefinitionObject).GetName();

	/// <summary>
	/// Gets the definition's ID.
	/// </summary>
	/// <remarks>
	/// The property is a copy of the <c>Id</c> note property, which stays in place.
	/// </remarks>
	/// <value>The ID that Sonarr assigned, copied when the object is deserialized, or 0 before then.</value>
	public int Id { get; private set; }
	/// <summary>
	/// Gets or sets the definition's title.
	/// </summary>
	/// <remarks>
	/// The property reads and writes the <c>Title</c> note property, so a change shows in PowerShell and in the next
	/// request body.
	/// </remarks>
	/// <value>The value of the <c>Title</c> note property, or an empty string when the object has none.</value>
	public string Title
	{
		get => this.GetStringOrEmpty();
		set => this.SetValue(value);
	}

	/// <summary>
	/// Initializes a new <see cref="QualityDefinitionObject"/> instance with no properties, which the JSON converter
	/// fills.
	/// </summary>
	public QualityDefinitionObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Compares this definition with another by ID.
	/// </summary>
	/// <param name="other">The definition to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this definition's <see cref="Id"/> is less than that of <paramref name="other"/>, 0 if
	/// they're equal, or a positive number if it's greater or <paramref name="other"/> is <see langword="null"/>.
	/// </returns>
	public int CompareTo(QualityDefinitionObject? other)
	{
		return Comparer<int?>.Default.Compare(this.Id, other?.Id);
	}

	/// <summary>
	/// Gets the quality definition tag from a resolver.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which this method ignores.</param>
	/// <returns>The resolver's <see cref="Meta.QUALITY_DEFINITION"/> tag.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.QUALITY_DEFINITION];
	}
	/// <summary>
	/// Finishes the object after the JSON converter reads it, and copies its ID.
	/// </summary>
	/// <remarks>
	/// The base class's steps run first, and then the <c>Id</c> note property is copied into <see cref="Id"/>.
	/// </remarks>
	public override void OnDeserialized()
	{
		base.OnDeserialized();
		if (this.TryGetId(out int id))
		{
			this.Id = id;
		}
	}
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
