namespace MG.Sonarr.Next.Json.Converters.Spans
{
    public sealed class AlwaysStringConverter : SpanConverter<string>
    {
        public override string? ConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName, bool isNull)
        {
            return new string(chars.Trim());
        }
    }
}
