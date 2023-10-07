using MG.Sonarr.Next.Services.Attributes;

namespace MG.Sonarr.Next.Services.Extensions
{
    public static class StringExtensions
    {
        [DebuggerStepThrough]
        public static SplitEnumerator SpanSplit([ValidatedNotNull] this ReadOnlySpan<char> value, in char splitBy)
        {
            return SpanSplit(value, new ReadOnlySpan<char>(in splitBy));
        }

        [DebuggerStepThrough]
        public static SplitEnumerator SpanSplit([ValidatedNotNull] this ReadOnlySpan<char> value, ReadOnlySpan<char> splitBy)
        {
            return new SplitEnumerator(value, splitBy);
        }

        [DebuggerStepThrough]
        public static DoubleSplitEnumerator SpanSplit([ValidatedNotNull] this ReadOnlySpan<char> value, ReadOnlySpan<char> splitBy1, ReadOnlySpan<char> splitBy2)
        {
            return new DoubleSplitEnumerator(value, splitBy1, splitBy2);
        }
    }
}
