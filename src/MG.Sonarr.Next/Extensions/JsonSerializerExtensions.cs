namespace MG.Sonarr.Next.Extensions
{
    public static class JsonSerializerExtensions
    {
        public static string ConvertName(this JsonSerializerOptions? options, string name)
        {
            name ??= string.Empty;
            return options?.PropertyNamingPolicy?.ConvertName(name) ?? name;
        }
    }
}
