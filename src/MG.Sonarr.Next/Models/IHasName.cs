namespace MG.Sonarr.Next.Models;

/// <summary>
/// Represents an object that has a name, which cmdlets match against the names they're given.
/// </summary>
public interface IHasName
{
	/// <summary>
	/// Gets the object's name.
	/// </summary>
	/// <value>The object's name.</value>
	string Name { get; }
}
