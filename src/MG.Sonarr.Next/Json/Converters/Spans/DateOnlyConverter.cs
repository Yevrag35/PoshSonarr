namespace MG.Sonarr.Next.Services.Json.Converters.Spans
{
    public sealed class TimeOnlyConverter : SpanConverter<TimeOnly>
    {
        public override TimeOnly ConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName, bool isNull)
        {
            if (TimeOnly.TryParse(chars, Statics.DefaultProvider, DateTimeStyles.AssumeUniversal, out TimeOnly @to))
            {
                return @to;
            }
            else if (DateTimeOffset.TryParse(chars, Statics.DefaultProvider, DateTimeStyles.AssumeUniversal, out DateTimeOffset tempOff))
            {
                return TimeOnly.FromDateTime(tempOff.UtcDateTime);
            }

            return default;
        }
    }

    public sealed class DateOnlyConverter : SpanConverter<DateOnly>
    {
        public override DateOnly ConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName, bool isNull)
        {
            if (DateOnly.TryParse(chars, Statics.DefaultProvider, DateTimeStyles.AssumeUniversal, out DateOnly @do))
            {
                return @do;
            }
            else if (DateTimeOffset.TryParse(chars, Statics.DefaultProvider, DateTimeStyles.AssumeUniversal, out DateTimeOffset tempOff))
            {
                return DateOnly.FromDateTime(tempOff.DateTime);
            }

            return @do;
        }
    }
}
