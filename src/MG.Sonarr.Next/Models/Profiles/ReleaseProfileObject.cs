using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Profiles;

/// <summary>
/// Represents a Sonarr release profile, from the <c>/releaseprofile</c> endpoint.
/// </summary>
/// <remarks>
/// The profile's <c>Ignored</c> and <c>Required</c> terms arrive as a
/// <see cref="MG.Sonarr.Next.Collections.StringSet"/>, and its <c>Preferred</c> terms as a
/// <see cref="MG.Sonarr.Next.Collections.StringKeyValueSet{T}"/> of <see cref="int"/> scores.
/// </remarks>
[SonarrObject]
public sealed class ReleaseProfileObject : TagUpdateObject<ReleaseProfileObject>,
	ISerializableNames<ReleaseProfileObject>
{
	/// <summary>
	/// The type's full name, which <see cref="SetPSTypeName"/> puts first in the PowerShell type names.
	/// </summary>
	static readonly string _typeName = typeof(ReleaseProfileObject).GetName();

	/// <summary>
	/// Gets or sets the profile's name.
	/// </summary>
	/// <remarks>
	/// The property reads and writes the <c>Name</c> note property, so a change shows in PowerShell and in the next
	/// request body.
	/// </remarks>
	/// <value>The value of the <c>Name</c> note property, or an empty string when the object has none.</value>
	public string Name
	{
		get => this.GetStringOrEmpty();
		set => this.SetValue(value);
	}

	/// <summary>
	/// Initializes a new <see cref="ReleaseProfileObject"/> instance with no properties, which the JSON converter
	/// fills.
	/// </summary>
	public ReleaseProfileObject()
		: base(10)
	{
	}

	/// <summary>
	/// Gets the release profile tag from a resolver.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which this method ignores.</param>
	/// <returns>The resolver's <see cref="Meta.RELEASE_PROFILE"/> tag.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.RELEASE_PROFILE];
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
