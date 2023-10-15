namespace MG.Sonarr.Next.Json.Converters.Spans
{
    public sealed class UtcTimeToLocalConverter : SpanConverter<DateTimeOffset>
    {
        public override bool CanConvert(ReadOnlySpan<char> propertyName)
        {
            return true;
        }

        public override DateTimeOffset ConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName, bool isNull)
        {
            DateTimeStyles styles = DateTimeStyles.AdjustToUniversal;
            if (propertyName.EndsWith(stackalloc char[] { 'U', 'T', 'C'}, StringComparison.InvariantCultureIgnoreCase))
            {
                styles = DateTimeStyles.AssumeUniversal;
            }

            _ = DateTimeOffset.TryParse(chars, Statics.DefaultProvider, styles, out DateTimeOffset offset);
            return offset;
        }
    }
}
