using MG.Sonarr.Next.Attributes;

namespace MG.Sonarr.Next.Extensions
{
    public static class BooleanExtensions
    {
        public static int GetLength([ValidatedNotNull] this bool value)
        {
            return value ? bool.TrueString.Length : bool.FalseString.Length;
        }
        public static string ToString(this bool value, out int length)
        {
            string s = value.ToString();
            length = s.Length;
            return s;
        }
        public static bool TryFormat([ValidatedNotNull] this bool value, Span<char> span, bool lowerCase, out int written)
        {
            if (lowerCase)
            {
                ReadOnlySpan<char> boolStr = value.ToString();
                Span<char> scratch = stackalloc char[span.Length];
                written = boolStr.ToLower(scratch, Statics.DefaultCulture);
                return scratch.Slice(0, written).TryCopyTo(span);
            }
            else
            {
                return value.TryFormat(span, out written);
            }
        }
    }
}
