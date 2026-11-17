namespace MG.Sonarr.Next.Collections;

/// <summary>
/// Defines members for collections that expose a count and can be sorted in place.
/// </summary>
public interface ISortable
{
	/// <summary>
	/// Gets the number of items in the collection.
	/// </summary>
	/// <value>The number of items.</value>
	int Count { get; }

	/// <summary>
	/// Sorts the collection.
	/// </summary>
	void Sort();
}
