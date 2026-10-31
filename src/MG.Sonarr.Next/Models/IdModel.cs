namespace MG.Sonarr.Next.Models;

/// <summary>
/// Represents an object that holds only a Sonarr ID.
/// </summary>
public sealed record IdModel : IHasId
{
	/// <summary>
	/// Gets the ID.
	/// </summary>
	/// <value>The Sonarr database ID.</value>
	public required int Id { get; init; }
}
