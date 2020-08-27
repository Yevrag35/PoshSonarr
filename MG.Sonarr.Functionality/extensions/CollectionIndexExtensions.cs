using System;
using System.Collections;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality.Extensions
{
    internal static class CollectionIndexExtensions
    {
        /// <summary>
        /// Returns the index even if the index is negative.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="index">The index integer which will be calculated to its positive value.</param>
        /// <returns>
        ///     If <paramref name="index"/> is postive and in a valid range, then the returned <see cref="int"/> will be unchanged.
        ///     If <paramref name="index"/> is negative, the returned <see cref="int"/> index will start from the end and count backwards
        ///     by calculating the <see cref="ICollection.Count"/> plus the negative value.
        ///     If <paramref name="index"/> is not in a valid range even after calculations (positive or negative), the returned value
        ///     will be -1 (indicating a <see cref="ArgumentOutOfRangeException"/> would be thrown).
        /// </returns>
        internal static int GetPositiveIndex<T>(this IReadOnlyCollection<T> list, int index)
        {
            if (index < 0)
                index = list.Count + index;

            if (index < 0 || index >= list.Count)
                index = -1;

            return index;
        }

        /// <summary>
        /// Returns the index even if the index is negative.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="index">The index integer which will be calculated to its positive value.</param>
        /// <returns>
        ///     If <paramref name="index"/> is postive and in a valid range, then the returned <see cref="int"/> will be unchanged.
        ///     If <paramref name="index"/> is negative, the returned <see cref="int"/> index will start from the end and count backwards
        ///     by calculating the <see cref="ICollection.Count"/> plus the negative value.
        ///     If <paramref name="index"/> is not in a valid range even after calculations (positive or negative), the returned value
        ///     will be -1 (indicating a <see cref="ArgumentOutOfRangeException"/> would be thrown).
        /// </returns>
        internal static int GetPositiveIndex<T>(this IList list, int index)
        {
            if (index < 0)
                index = list.Count + index;

            if (index < 0 || index >= list.Count)
                index = -1;

            return index;
        }
    }
}
