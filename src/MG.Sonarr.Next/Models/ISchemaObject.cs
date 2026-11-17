using MG.Sonarr.Next.Json;

namespace MG.Sonarr.Next.Models;

/// <summary>
/// Represents a schema object: a template from a Sonarr schema endpoint for a new resource.
/// </summary>
public interface ISchemaObject : IJsonSonarrMetadata
{
	/// <summary>
	/// Gets a value that indicates whether the resource that the schema creates takes tags.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if the resource takes tags; otherwise, <see langword="false"/>.
	/// </value>
	bool IsTaggable { get; }
}
