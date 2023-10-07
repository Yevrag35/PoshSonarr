namespace MG.Sonarr.Next.Json.Converters.Spans
{
    public abstract class SpanConverter
    {
        public abstract object? ConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName);
    }

    public abstract class SpanConverter<T> : SpanConverter
    {
        public sealed override object? ConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName)
        {
            return this.ConvertSpan(chars, propertyName, false);
        }

        public abstract T? ConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName, bool isNull);
    }
}
