using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models;

/// <summary>
/// Represents one page of records from a paged Sonarr endpoint, such as <c>/history</c>, <c>/log</c>, or
/// <c>/wanted/missing</c>.
/// </summary>
/// <remarks>
/// The HTTP client tags each record through <see cref="SetTag(IMetadataResolver)"/> and sorts the records.
/// </remarks>
/// <typeparam name="T">The type of the records.</typeparam>
public sealed class RecordResult<T> : IJsonMetadataTaggable, ISortable
	where T : IComparable<T>, IJsonMetadataTaggable
{
	/// <summary>
	/// Gets the number of records on the page.
	/// </summary>
	/// <value>The number of items in <see cref="Records"/>.</value>
	int ISortable.Count => this.Records.Count;
	/// <summary>
	/// Gets the page's number.
	/// </summary>
	/// <value>The 1-based number of the page.</value>
	public int Page { get; init; }
	/// <summary>
	/// Gets the number of records on a page.
	/// </summary>
	/// <value>The page size that the request asked for.</value>
	public int PageSize { get; init; }
	/// <summary>
	/// Gets the number of records across all pages.
	/// </summary>
	/// <value>The total number of records that the endpoint holds for the request.</value>
	public int TotalRecords { get; init; }
	/// <summary>
	/// Gets the page's records.
	/// </summary>
	/// <value>The list of the page's records.</value>
	public required MetadataList<T> Records { get; init; }

	/// <summary>
	/// Tags each of the page's records.
	/// </summary>
	/// <param name="resolver">The resolver that holds the tags that the connection registered.</param>
	public void SetTag(IMetadataResolver resolver)
	{
		this.Records.SetTag(resolver);
	}
	/// <summary>
	/// Sorts the page's records in their default order.
	/// </summary>
	void ISortable.Sort()
	{
		this.Records.Sort();
	}
}
