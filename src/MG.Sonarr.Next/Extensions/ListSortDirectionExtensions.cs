using System.ComponentModel;

namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Custom extension methods for <see cref="ListSortDirection"/>.
/// </summary>
public static class ListSortDirectionExtensions
{
	static readonly int s_aSCENDING_LENGTH = nameof(ListSortDirection.Ascending).Length;
	static readonly int s_dESCENDING_LENGTH = nameof(ListSortDirection.Descending).Length;

	/// <summary>
	/// Gets the number of characters the current <see cref="ListSortDirection"/> value consists of
	/// if converted to a <see cref="string"/> instance.
	/// </summary>
	/// <param name="direction">The direction whose length is returned.</param>
	/// <returns>
	///     The number of characters in the <see cref="ListSortDirection"/> value's length.
	/// </returns>
	public static int GetLength(this ListSortDirection direction)
	{
		return direction switch
		{
			ListSortDirection.Ascending => s_aSCENDING_LENGTH,
			ListSortDirection.Descending => s_dESCENDING_LENGTH,
			_ => ((int)direction).GetLength(),
		};
	}
}
