namespace MG.Sonarr.Next.Models.Series;

/// <summary>
/// Represents an image associated with a series, including its cover type and URL.
/// </summary>
public sealed record SeriesImage : IComparable<SeriesImage>
{
	/// <summary>
	/// Gets or sets the type of cover image (e.g., "poster", "banner", etc.).
	/// </summary>
	public required string CoverType { get; init; }
	/// <summary>
	/// Gets or sets the URL of the image.
	/// </summary>
	public required string Url { get; init; }

	/// <summary>
	/// Compares the current <see cref="SeriesImage"/> instance with another <see cref="SeriesImage"/> instance based on the <see cref="CoverType"/>.
	/// </summary>
	/// <param name="other">The other <see cref="SeriesImage"/> instance to compare with.</param>
	/// <returns>
	/// A value less than zero if this instance precedes <paramref name="other"/> in the sort order,
	/// zero if this instance occurs in the same position in the sort order as <paramref name="other"/>,
	/// or a value greater than zero if this instance follows <paramref name="other"/> in the sort order.
	/// </returns>
	public int CompareTo(SeriesImage? other)
	{
		return StringComparer.InvariantCultureIgnoreCase.Compare(this.CoverType, other?.CoverType);
	}
}
