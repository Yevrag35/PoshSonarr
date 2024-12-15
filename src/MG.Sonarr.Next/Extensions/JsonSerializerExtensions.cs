namespace MG.Sonarr.Next.Extensions
{
    public static class JsonSerializerExtensions
    {
        [return: NotNullIfNotNull(nameof(name))]
        public static string? ConvertName(this JsonSerializerOptions? options, string? name)
        {
            if (string.IsNullOrWhiteSpace(name) || options is null || options.PropertyNamingPolicy is null)
            {
                return name;
            }

            return options.PropertyNamingPolicy.ConvertName(name);
        }

        [Obsolete("Complete rework", error: true)]
        public static bool HasCamelCaseNamingPolicy([NotNullWhen(true)] this JsonSerializerOptions? options)
        {
            return ReferenceEquals(JsonNamingPolicy.CamelCase, options?.PropertyNamingPolicy);
        }
    }
}
