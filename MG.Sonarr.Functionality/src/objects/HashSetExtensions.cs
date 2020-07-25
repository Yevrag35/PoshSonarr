using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality.Extensions
{
    /// <summary>
    /// A static class for extending <see cref="HashSet{T}"/> to allow adding multiple values at once.
    /// </summary>
    public static class HashSetExtensions
    {
        /// <summary>
        /// Adds the members of the specified collection to the <see cref="HashSet{T}"/>.
        /// </summary>
        /// <typeparam name="T">The .NET type of each member of the <see cref="HashSet{T}"/>.</typeparam>
        /// <param name="hashSet">The set to extend.</param>
        /// <param name="values">The collection whose values will be added to the <see cref="HashSet{T}"/>.</param>
        /// <returns>
        ///     <see langword="true"/>: if all members of <paramref name="values"/> were able to be added to the set;
        ///     <see langword="false"/>, otherwise.
        ///     If <paramref name="values"/> contains no elements, then <see langword="true"/> is returned.
        /// </returns>
        public static bool AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> values)
        {
            bool result = true;
            foreach (T val in values)
            {
                bool check = hashSet.Add(val);
                if (!check)
                    result = false;
            }
            return result;
        }
    }
}
