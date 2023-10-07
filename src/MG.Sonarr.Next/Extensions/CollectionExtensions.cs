namespace MG.Sonarr.Next.Services.Extensions
{
    public static class CollectionExtensions
    {
        public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IReadOnlyCollection<T>? readOnlyCol)
        {
            return readOnlyCol is null || readOnlyCol.Count <= 0;
        }
    }
}
