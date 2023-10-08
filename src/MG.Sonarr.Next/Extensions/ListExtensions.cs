using MG.Sonarr.Next.Attributes;

namespace MG.Sonarr.Next.Extensions
{
    /// <summary>
    /// Custom extension methods for <see cref="List{T}"/> and <see cref="IList{T}"/> implementations.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Adds all elements from the specified array into the current list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to copy the elements into.</param>
        /// <param name="values">
        ///     The array of elements that will be added into <paramref name="list"/>.
        /// </param>
        /// <exception cref="ArgumentNullException"/>
        public static void AddMany<T>(this IList<T> list, params T[] values)
        {
            AddMany(list, collection: values);
        }

        /// <summary>
        /// Adds all elements from the specified collection into the current list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to copy the elements into.</param>
        /// <param name="collection">
        ///     The collection of elements that will be added into <paramref name="list"/>.
        /// </param>
        /// <exception cref="ArgumentNullException"/>
        public static void AddMany<T>([ValidatedNotNull] this IList<T> list, IEnumerable<T>? collection)
        {
            ArgumentNullException.ThrowIfNull(list);
            if (collection is null)
            {
                return;
            }

            foreach (T value in collection)
            {
                list.Add(value);
            }
        }

        /// <summary>
        /// Removes all elements of a <see cref="IList{T}"/> that match the specified
        /// predicate.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list whose elements will be conditionally removed.</param>
        /// <param name="predicate">
        ///     A delegate that determines if an individual element in <paramref name="list"/> should be
        ///     removed.
        /// </param>
        /// <returns>
        ///     The number of items that were removed from <paramref name="list"/>.
        /// </returns>
        [DebuggerStepThrough]
        public static int RemoveWhere<T>(this IList<T>? list, Func<T, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);

            return RemoveWhere(list, predicate, indexedPredicate: (index, item, state) => 
                state!.Invoke(item));
        }
        /// <summary>
        /// Removes all elements of a <see cref="IList{T}"/> that match the specified
        /// predicate.
        /// </summary>
        /// <remarks>
        ///     The first parameter in the predicate is the index of the current item.
        /// </remarks>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list whose elements will be conditionally removed.</param>
        /// <param name="indexPredicate">
        ///     A delegate that determines if an individual element in <paramref name="list"/> should be
        ///     removed.
        /// </param>
        /// <returns>
        ///     The number of items that were removed from <paramref name="list"/>.
        /// </returns>
        [DebuggerStepThrough]
        public static int RemoveWhere<T>(this IList<T>? list, [ValidatedNotNull] Func<int, T, bool> indexPredicate)
        {
            ArgumentNullException.ThrowIfNull(indexPredicate);

            return RemoveWhere(list, indexPredicate, (index, item, state) => state!.Invoke(index, item));
        }
        /// <summary>
        /// Removes all elements of a <see cref="IList{T}"/> that match the specified
        /// predicate.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="TState">The type of the state provided.</typeparam>
        /// <param name="list">The list whose elements will be conditionally removed.</param>
        /// <param name="state">A provided state object to prevent closures.</param>
        /// <param name="predicate">
        ///     A delegate that determines if an individual element in <paramref name="list"/> should be
        ///     removed.
        /// </param>
        /// <returns>
        ///     The number of items that were removed from <paramref name="list"/>.
        /// </returns>
        public static int RemoveWhere<T, TState>(this IList<T>? list, TState? state, [ValidatedNotNull] Func<T, TState?, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);

            if (list is null || list.Count <= 0)
            {
                return 0;
            }

            int removed = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (predicate(list[i], state))
                {
                    list.RemoveAt(i);
                    removed++;
                }
            }

            return removed;
        }
        /// <summary>
        /// Removes all elements of a <see cref="IList{T}"/> that match the specified
        /// predicate.
        /// </summary>
        /// <remarks>
        ///     The first parameter in the predicate is the index of the current item.
        /// </remarks>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="TState">The type of the state provided.</typeparam>
        /// <param name="list">The list whose elements will be conditionally removed.</param>
        /// <param name="state">A provided state object to prevent closures.</param>
        /// <param name="indexedPredicate">
        ///     A delegate that determines if an individual element in <paramref name="list"/> should be
        ///     removed.
        /// </param>
        /// <returns>
        ///     The number of items that were removed from <paramref name="list"/>.
        /// </returns>
        public static int RemoveWhere<T, TState>(this IList<T>? list, TState? state, Func<int, T, TState?, bool> indexedPredicate)
        {
            ArgumentNullException.ThrowIfNull(indexedPredicate);

            if (list is null || list.Count <= 0)
            {
                return 0;
            }

            int removed = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (indexedPredicate(i, list[i], state))
                {
                    list.RemoveAt(i);
                    removed++;
                }
            }

            return removed;
        }
    }
}
