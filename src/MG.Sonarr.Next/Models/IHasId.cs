namespace MG.Sonarr.Next.Models;

/// <summary>
/// Represents an object that has a Sonarr ID.
/// </summary>
public interface IHasId
{
	/// <summary>
	/// Gets the object's ID.
	/// </summary>
	/// <value>The Sonarr database ID of the object.</value>
	int Id { get; }
}
