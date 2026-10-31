namespace MG.Sonarr.Next;

/// <summary>
/// Provides query parameter names used for paged Sonarr requests.
/// </summary>
public static class PagingConstants
{
	/// <summary>
	/// The page size query parameter name.
	/// </summary>
	public const string PageSize = "pageSize";

	/// <summary>
	/// The page number query parameter name.
	/// </summary>
	public const string PageNumber = "page";

	/// <summary>
	/// The sort direction query parameter name.
	/// </summary>
	public const string SortDirection = "sortDirection";

	/// <summary>
	/// The sort key query parameter name.
	/// </summary>
	public const string SortKey = "sortKey";
}
