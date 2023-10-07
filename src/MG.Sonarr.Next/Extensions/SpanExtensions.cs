using MG.Sonarr.Next.Attributes;

namespace MG.Sonarr.Next.Extensions
{
    public static class CharSpanExtensions
    {
        [DebuggerStepThrough]
        public static void CopyToSlice(this string? value, Span<char> span, ref int position)
        {
            CopyToSlice(spanValue: value.AsSpan(), span, ref position);
        }
        public static void CopyToSlice([ValidatedNotNull] this ReadOnlySpan<char> spanValue, Span<char> span, ref int position)
        {
            if (spanValue.IsEmpty)
            {
                return;
            }

            spanValue.CopyTo(span.Slice(position));
            position += spanValue.Length;
        }

        [DebuggerStepThrough]
        public static void CopyToSlice([ValidatedNotNull] this Span<char> writtableSpan, Span<char> span, ref int position)
        {
            CopyToSlice(spanValue: writtableSpan, span, ref position);
        }

        public static bool TryCopyToSlice([ValidatedNotNull] this ReadOnlySpan<char> spanValue, Span<char> span, ref int position)
        {
            if (!spanValue.IsEmpty && spanValue.TryCopyTo(span.Slice(position)))
            {
                position += spanValue.Length;
                return true;
            }
            else
            {
                return false;
            }
        }
        [DebuggerStepThrough]
        public static bool TryCopyToSlice(this Span<char> writtableSpan, Span<char> span, ref int position)
        {
            return TryCopyToSlice(spanValue: writtableSpan, span, ref position);
        }

        [DebuggerStepThrough]
        public static bool StartsWith([ValidatedNotNull] this ReadOnlySpan<char> span, in char value, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
        {
            return span.StartsWith(new ReadOnlySpan<char>(in value), comparison);
        }
        [DebuggerStepThrough]
        public static bool StartsWith([ValidatedNotNull] this Span<char> span, in char value)
        {
            return span.StartsWith(new ReadOnlySpan<char>(in value));
        }
        public static bool StartsWith([ValidatedNotNull] this Span<char> span, in char value, bool ignoreCase)
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
