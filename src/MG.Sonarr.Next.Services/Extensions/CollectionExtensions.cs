namespace MG.Sonarr.Next.Services.Extensions
{
    public static class CollectionExtensions
    {
        public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IReadOnlyCollection<T>? readOnlyCol)
        {
            return readOnlyCol is null || readOnlyCol.Count <= 0;
        }

        //public static bool IsNullOrEmpty<T>([NotNullWhen(true)] this ICollection<T>? col)
        //{
        //    return col is null || col.Count <= 0;
        //}
    }
}
