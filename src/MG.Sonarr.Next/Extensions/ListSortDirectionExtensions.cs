using System.ComponentModel;

namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ListSortDirection"/>.
/// </summary>
public static class ListSortDirectionExtensions
{
	/// <summary>
	/// The number of characters in the name <c>Ascending</c>.
	/// </summary>
	static readonly int s_aSCENDING_LENGTH = nameof(ListSortDirection.Ascending).Length;
	/// <summary>
	/// The number of characters in the name <c>Descending</c>.
	/// </summary>
	static readonly int s_dESCENDING_LENGTH = nameof(ListSortDirection.Descending).Length;

	/// <summary>
	/// Gets the number of characters in the text of a sort direction, without formatting it.
	/// </summary>
	/// <param name="direction">The direction to measure.</param>
	/// <returns>
	/// The length of the direction's name, or, for a value that <see cref="ListSortDirection"/> doesn't name, the
	/// length of its number in decimal, which is what <see cref="Enum.ToString()"/> writes for it.
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
