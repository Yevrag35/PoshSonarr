using System.ComponentModel;

namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ListSortDirection"/>.
/// </summary>
public static class ListSortDirectionExtensions
{
	private const int ASCENDING_LENGTH = 9;     // Number of characters in "Ascending"
	private const int DESCENDING_LENGTH = 10;   // Number of characters in "Descending"

#if DEBUG
	static ListSortDirectionExtensions()
	{
		Debug.Assert(ASCENDING_LENGTH == nameof(ListSortDirection.Ascending).Length, $"{nameof(ASCENDING_LENGTH)} ({ASCENDING_LENGTH}) does not match the length of {nameof(ListSortDirection.Ascending)}");
		Debug.Assert(DESCENDING_LENGTH == nameof(ListSortDirection.Descending).Length, $"{nameof(DESCENDING_LENGTH)} ({DESCENDING_LENGTH}) does not match the length of {nameof(ListSortDirection.Descending)}");
	}
#endif

	/// <summary>
	/// Extensions for <see cref="ListSortDirection"/> values.
	/// </summary>
	/// <param name="direction">The direction to extend.</param>
	extension(ListSortDirection direction)
	{
		/// <summary>
		/// Gets the number of characters in the text of a sort direction, without formatting it.
		/// </summary>
		/// <param name="direction">The direction to measure.</param>
		/// <returns>
		/// The length of the direction's name, or, for a value that <see cref="ListSortDirection"/> doesn't name, the
		/// length of its number in decimal, which is what <see cref="Enum.ToString()"/> writes for it.
		/// </returns>
		public int Length
		{
			get
			{
				return direction switch
				{
					ListSortDirection.Ascending => ASCENDING_LENGTH,
					ListSortDirection.Descending => DESCENDING_LENGTH,
					_ => ((int)direction).GetLength(),
				};
			}
		}
	}
}
