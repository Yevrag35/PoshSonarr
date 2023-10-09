namespace MG.Sonarr.Next.Json.Converters.Spans
{
    public sealed class TimeSpanConverter : SpanConverter<TimeSpan>
    {
        public override TimeSpan ConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName, bool isNull)
        {
            if (TimeSpan.TryParse(chars, Statics.DefaultProvider, out TimeSpan timeSpan))
            {
                return timeSpan;
            }

            return default;
        }
    }
}
