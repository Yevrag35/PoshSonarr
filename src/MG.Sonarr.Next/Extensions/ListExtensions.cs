namespace MG.Sonarr.Next.Extensions
{
    public static class ListExtensions
    {
        public static void AddMany<T>(this IList<T> list, params T[] values)
        {
            AddMany(list, collection: values);
        }

        /// <exception cref="ArgumentNullException"/>
        public static void AddMany<T>(this IList<T> list, IEnumerable<T>? collection)
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
    }
}
