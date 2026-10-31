using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Config;

/// <summary>
/// Represents Sonarr's download client settings, the single object at <c>/config/downloadclient</c>.
/// </summary>
/// <remarks>
/// The object carries the <see cref="Meta.DOWNLOAD_CLIENT_CONFIG"/> tag. Its <c>Id</c> property stays read-only in
/// PowerShell, and <see cref="OnSerializing"/> rewrites it from <see cref="IdSonarrObject{TSelf}.Id"/> before the
/// object is sent.
/// </remarks>
[SonarrObject]
public sealed class DownloadClientConfigObject : IdSonarrObject<DownloadClientConfigObject>,
	IJsonOnSerializing,
	ISerializableNames<DownloadClientConfigObject>
{
	/// <summary>
	/// The full name of the class, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(DownloadClientConfigObject).GetName();
	/// <summary>
	/// The number of properties that an instance is sized for, about the number of fields that Sonarr sends.
	/// </summary>
	const int CAPACITY = 5;

	/// <summary>
	/// Initializes a new <see cref="DownloadClientConfigObject"/> instance with no properties, for the JSON converter
	/// to fill.
	/// </summary>
	public DownloadClientConfigObject()
		: base(CAPACITY)
	{
	}
	/// <summary>
	/// Gets the metadata tag for the download client settings.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which the method ignores.</param>
	/// <returns>The <see cref="Meta.DOWNLOAD_CLIENT_CONFIG"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.DOWNLOAD_CLIENT_CONFIG];
	}

	/// <summary>
	/// Writes <see cref="IdSonarrObject{TSelf}.Id"/> into the <c>Id</c> property before the object is serialized.
	/// </summary>
	/// <remarks>
	/// The property is replaced with a read-only one when it's read-only, so PowerShell still can't change it
	/// afterward.
	/// </remarks>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Because of 'nameof()'")]
	public void OnSerializing()
	{
		this.UpdateProperty(this.Id, replaceReadOnly: true, propertyName: nameof(Id));
	}

	/// <summary>
	/// Inserts the full name of <see cref="DownloadClientConfigObject"/> at the start of the object's type names, ahead
	/// of the base class's name.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}
}
