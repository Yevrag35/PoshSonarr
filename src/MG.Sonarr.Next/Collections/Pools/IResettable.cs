namespace MG.Sonarr.Next.Collections.Pools;

/// <summary>
/// An interface for objects that can be reset to their initial state.
/// </summary>
public interface IResettable
{
	/// <summary>
	/// Attempts to reset the object to its initial state.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if the object was successfully reset; otherwise, <see langword="false"/>.
	/// </returns>
	bool TryReset();
}
