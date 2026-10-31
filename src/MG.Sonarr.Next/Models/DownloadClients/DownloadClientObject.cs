using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.DownloadClients;

/// <summary>
/// Represents a download client that Sonarr sends releases to.
/// </summary>
/// <remarks>
/// The object carries the <see cref="Meta.DOWNLOAD_CLIENT"/> tag, and pipes its ID as an <see cref="ITestPipeable"/>,
/// so the client can be tested.
/// </remarks>
[SonarrObject]
public sealed class DownloadClientObject : TagUpdateObject<DownloadClientObject>,
	ISerializableNames<DownloadClientObject>,
	ITestPipeable
{
	/// <summary>
	/// The number of properties that an instance is sized for, about the number of fields that Sonarr sends.
	/// </summary>
	const int CAPACITY = 15;
	/// <summary>
	/// The full name of the class, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(DownloadClientObject).GetName();

	/// <summary>
	/// Gets or sets the download client's name.
	/// </summary>
	/// <remarks>
	/// The property reads and writes the <c>Name</c> property, so it follows a change that PowerShell makes.
	/// </remarks>
	/// <value>The value of the <c>Name</c> property, or an empty string when the object has none.</value>
	public string Name
	{
		get => this.GetStringOrEmpty();
		set => this.SetValue(value);
	}

	/// <summary>
	/// Initializes a new <see cref="DownloadClientObject"/> instance with no properties, for the JSON converter to
	/// fill.
	/// </summary>
	public DownloadClientObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Gets the metadata tag for a download client.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which the method ignores.</param>
	/// <returns>The <see cref="Meta.DOWNLOAD_CLIENT"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.DOWNLOAD_CLIENT];
	}

	/// <summary>
	/// Inserts the full name of <see cref="DownloadClientObject"/> at the start of the object's type names, ahead of
	/// the base class's name.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}

	/// <summary>
	/// Gets the ID of the download client to test.
	/// </summary>
	/// <returns>The download client's <see cref="IdSonarrObject{TSelf}.Id"/>.</returns>
	int? IPipeable<ITestPipeable>.GetId()
	{
		return this.Id;
	}
}
