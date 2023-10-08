namespace MG.Sonarr.Next.Extensions
{
    /// <summary>
    /// Custom extension methods for certain collection interfaces.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Determines whether the current collection is <see langword="null"/> or contains 0 elements.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="collection">
        ///     The collection to check. If <see langword="false"/> is returned, this has 
        ///     been determined to be not <see langword="null"/>.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the collection is not <see langword="null"/> and contains
        ///     at least 1 element; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IReadOnlyCollection<T>? collection)
        {
            return collection is null || collection.Count <= 0;
        }
    }
}
