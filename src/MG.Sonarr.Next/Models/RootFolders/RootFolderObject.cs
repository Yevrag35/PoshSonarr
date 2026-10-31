using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.RootFolders;

/// <summary>
/// Represents a Sonarr root folder, a folder that series are added under, from the <c>/rootfolder</c> endpoint.
/// </summary>
[SonarrObject]
public sealed class RootFolderObject : IdSonarrObject<RootFolderObject>,
	ISerializableNames<RootFolderObject>
{
	/// <summary>
	/// The number of properties that a new instance makes room for.
	/// </summary>
	const int CAPACITY = 9;
	/// <summary>
	/// The type's full name, which <see cref="SetPSTypeName"/> puts first in the PowerShell type names.
	/// </summary>
	static readonly string _typeName = typeof(RootFolderObject).GetName();

	/// <summary>
	/// Initializes a new <see cref="RootFolderObject"/> instance with no properties, which the JSON converter fills.
	/// </summary>
	public RootFolderObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Gets the root folder tag from a resolver.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which this method ignores.</param>
	/// <returns>The resolver's <see cref="Meta.ROOT_FOLDER"/> tag.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.ROOT_FOLDER];
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
