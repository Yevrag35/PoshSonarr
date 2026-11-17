using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models;

/// <summary>
/// Represents a type whose metadata tag can be resolved without an instance.
/// </summary>
/// <typeparam name="T">The type that the tag belongs to.</typeparam>
public interface ITagResolvable<T> where T : IJsonMetadataTaggable
{
	/// <summary>
	/// When implemented in a derived class, gets the metadata tag of <typeparamref name="T"/>.
	/// </summary>
	/// <param name="resolver">The resolver that holds the tags that the connection registered.</param>
	/// <returns>The metadata tag of <typeparamref name="T"/>.</returns>
	static abstract MetadataTag GetTag(IMetadataResolver resolver);
}
