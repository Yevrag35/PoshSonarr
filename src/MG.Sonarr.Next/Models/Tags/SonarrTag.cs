using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Tags;

/// <summary>
/// Represents the body of a request that creates a tag.
/// </summary>
/// <remarks>
/// An <see cref="Id"/> of 0 is left out of the body, as it is for a tag that doesn't exist yet, and
/// <see cref="MetadataTag"/> is never written.
/// </remarks>
public sealed record SonarrTag : IHasId, IJsonSonarrMetadata
{
	/// <summary>
	/// Gets the tag's ID.
	/// </summary>
	/// <value>The tag's ID, or 0 for a tag that doesn't exist yet, which leaves the field out of a request body.</value>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public required int Id { get; init; }
	/// <summary>
	/// Gets the tag's label.
	/// </summary>
	/// <value>The label.</value>
	public required string Label { get; init; }

	/// <summary>
	/// Gets the tag's metadata tag.
	/// </summary>
	/// <value>
	/// The <see cref="Meta.TAG"/> tag once <see cref="SetTag(IMetadataResolver)"/> has run; otherwise,
	/// <see cref="MetadataTag.Empty"/>.
	/// </value>
	[JsonIgnore(Condition = JsonIgnoreCondition.Always)]
	public MetadataTag MetadataTag { get; private set; } = MetadataTag.Empty;

	/// <summary>
	/// Sets <see cref="MetadataTag"/> to the tag tag from a resolver.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	public void SetTag(IMetadataResolver resolver)
	{
		this.MetadataTag = resolver[Meta.TAG];
	}
}
