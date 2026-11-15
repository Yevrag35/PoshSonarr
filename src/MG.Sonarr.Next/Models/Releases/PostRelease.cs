namespace MG.Sonarr.Next.Models.Releases;

/// <summary>
/// Represents the body of a request that grabs a release that a search found.
/// </summary>
public sealed class PostRelease
{
	/// <summary>
	/// Gets the release's identifier, from Sonarr's <c>guid</c> field.
	/// </summary>
	/// <value>The <see cref="ReleaseObject.ReleaseUrl"/> of the release to grab.</value>
	public required string Guid { get; init; }
	/// <summary>
	/// Gets the ID of the indexer that found the release.
	/// </summary>
	/// <value>The <see cref="ReleaseObject.IndexerId"/> of the release to grab.</value>
	public required int IndexerId { get; init; }
}
