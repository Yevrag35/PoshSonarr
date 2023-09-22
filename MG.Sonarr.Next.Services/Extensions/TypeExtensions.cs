namespace MG.Sonarr.Next.Services.Extensions
{
    public static class TypeExtensions
    {
        [return: NotNullIfNotNull(nameof(type))]
        public static string? GetTypeName(this Type? type)
        {
            return type?.FullName ?? type?.Name;
        }
    }
}
