using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Qualities;

/// <summary>
/// Represents a Sonarr quality profile, from the <c>/qualityprofile</c> endpoint.
/// </summary>
[SonarrObject]
public sealed class QualityProfileObject : IdSonarrObject<QualityProfileObject>,
	ISerializableNames<QualityProfileObject>
{
	/// <summary>
	/// The number of properties that a new instance makes room for.
	/// </summary>
	const int CAPACITY = 10;
	/// <summary>
	/// The type's full name, which <see cref="SetPSTypeName"/> puts first in the PowerShell type names.
	/// </summary>
	static readonly string _typeName = typeof(QualityProfileObject).GetName();

	/// <summary>
	/// Gets the profile's name.
	/// </summary>
	/// <remarks>
	/// The property is a copy of the <c>Name</c> note property, taken when the object is deserialized, so it doesn't
	/// follow a change that PowerShell makes to the note property afterward.
	/// </remarks>
	/// <value>The profile's name, or an empty string when Sonarr sent none.</value>
	public string Name { get; private set; } = string.Empty;

	/// <summary>
	/// Initializes a new <see cref="QualityProfileObject"/> instance with no properties, which the JSON converter
	/// fills.
	/// </summary>
	public QualityProfileObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Gets the quality profile tag from a resolver.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which this method ignores.</param>
	/// <returns>The resolver's <see cref="Meta.QUALITY_PROFILE"/> tag.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.QUALITY_PROFILE];
	}

	/// <summary>
	/// Copies the profile's name after the base class's deserialization steps.
	/// </summary>
	/// <param name="alreadyCalled">
	/// <see langword="true"/> if the object was deserialized before; otherwise, <see langword="false"/>.
	/// </param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		base.OnDeserialized(alreadyCalled);
		if (this.TryGetNonNullProperty(nameof(this.Name), out string? name))
		{
			this.Name = name;
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
