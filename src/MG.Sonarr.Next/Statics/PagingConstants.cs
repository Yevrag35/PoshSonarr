namespace MG.Sonarr.Next;

/// <summary>
/// Provides query parameter names used for paged Sonarr requests.
/// </summary>
internal static class PagingConstants
{
	/// <summary>
	/// The page size query parameter name.
	/// </summary>
	internal const string PageSize = "pageSize";

	/// <summary>
	/// The page number query parameter name.
	/// </summary>
	internal const string PageNumber = "page";

	/// <summary>
	/// The sort direction query parameter name.
	/// </summary>
	internal const string SortDirection = "sortDirection";

	/// <summary>
	/// The sort key query parameter name.
	/// </summary>
	internal const string SortKey = "sortKey";
}
