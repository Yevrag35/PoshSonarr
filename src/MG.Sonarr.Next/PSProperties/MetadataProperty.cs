using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Metadata;
using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties;

/// <summary>
/// Represents the read-only <c>MetadataTag</c> note property of a Sonarr object, which holds the object's
/// <see cref="MetadataTag"/>.
/// </summary>
/// <remarks>
/// The tag names the object's endpoint and the cmdlets that the object can be piped to.
/// </remarks>
public sealed class MetadataProperty : ReadOnlyProperty<MetadataTag>
{
	/// <summary>
	/// The full name of <see cref="MetadataTag"/>, which <see cref="TypeNameOfValue"/> returns.
	/// </summary>
	private static readonly string s_typeName = typeof(MetadataTag).GetName();
	/// <summary>
	/// Gets the metadata tag that the property holds.
	/// </summary>
	/// <value>The object's metadata tag.</value>
	public MetadataTag Tag { get; internal set; }
	/// <summary>
	/// Gets the full name of the property's value type.
	/// </summary>
	/// <value>The full name of <see cref="MetadataTag"/>.</value>
	public override string TypeNameOfValue => s_typeName;
	/// <summary>
	/// Gets the property's value as a <see cref="MetadataTag"/>.
	/// </summary>
	/// <value>The value of <see cref="Tag"/>.</value>
	protected override MetadataTag ValueAsT => this.Tag;

	/// <summary>
	/// Initializes a new <see cref="MetadataProperty"/> instance that holds <see cref="MetadataTag.Empty"/>.
	/// </summary>
	private MetadataProperty()
		: this(MetadataTag.Empty)
	{
	}
	/// <summary>
	/// Initializes a new <see cref="MetadataProperty"/> instance that holds the specified tag.
	/// </summary>
	/// <param name="value">The metadata tag that the property holds.</param>
	public MetadataProperty(MetadataTag value)
		: base(MetadataResolver.META_PROPERTY_NAME)
	{
		this.Tag = value;
	}

	/// <summary>
	/// The shared property that holds <see cref="MetadataTag.Empty"/>, whose name callers read to find an object's tag
	/// property.
	/// </summary>
	internal static readonly MetadataProperty Empty = new();

	/// <summary>
	/// Creates a copy of the property.
	/// </summary>
	/// <returns>A new <see cref="MetadataProperty"/> that holds the same <see cref="MetadataTag"/> instance.</returns>
	public override PSMemberInfo Copy()
	{
		return new MetadataProperty(this.Tag);
	}
}
