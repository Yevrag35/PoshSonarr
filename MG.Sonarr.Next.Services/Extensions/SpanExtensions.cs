namespace MG.Sonarr.Next.Services.Extensions
{
    public static class CharSpanExtensions
    {
        public static void CopyToSlice(this string? value, Span<char> span, ref int position)
        {
            CopyToSlice(spanValue: value.AsSpan(), span, ref position);
        }
        public static void CopyToSlice(this ReadOnlySpan<char> spanValue, Span<char> span, ref int position)
        {
            if (spanValue.IsEmpty)
            {
                return;
            }

            spanValue.CopyTo(span.Slice(position));
            position += spanValue.Length;
        }

        public static bool StartsWith(this ReadOnlySpan<char> span, in char value, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
        {
            return span.StartsWith(new ReadOnlySpan<char>(in value), comparison);
        }
        public static bool StartsWith(this Span<char> span, in char value)
        {
            return span.StartsWith(new ReadOnlySpan<char>(in value));
        }
        public static bool StartsWith(this Span<char> span, in char value, bool ignoreCase)
        {
            bool startsWith = StartsWith(span, in value);
            if (!startsWith && ignoreCase)
            {
                char other = char.IsUpper(value)
                    ? char.ToLower(value)
                    : char.ToUpper(value);

                return value != other && StartsWith(span, in other);
            }

            return startsWith;
        }
    }
}
