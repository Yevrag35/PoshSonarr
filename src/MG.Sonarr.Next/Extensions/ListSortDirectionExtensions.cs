using MG.Sonarr.Next.Attributes;
using System.ComponentModel;

namespace MG.Sonarr.Next.Extensions
{
    /// <summary>
    /// Custom extension methods for <see cref="ListSortDirection"/>.
    /// </summary>
    public static class ListSortDirectionExtensions
    {
        static readonly int ASCENDING_LENGTH = ListSortDirection.Ascending.ToString().Length;
        static readonly int DESCENDING_LENGTH = ListSortDirection.Descending.ToString().Length;

        /// <summary>
        /// Gets the number of characters the current <see cref="ListSortDirection"/> value consists of
        /// if converted to a <see cref="string"/> instance.
        /// </summary>
        /// <param name="direction">The direction whose length is returned.</param>
        /// <returns>
        ///     The number of characters in the <see cref="ListSortDirection"/> value's length.
        /// </returns>
        public static int GetLength([ValidatedNotNull] this ListSortDirection direction)
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
