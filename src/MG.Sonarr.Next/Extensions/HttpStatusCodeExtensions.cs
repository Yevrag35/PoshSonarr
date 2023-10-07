using MG.Sonarr.Next.Attributes;
using System.Collections.ObjectModel;
using System.Net;

namespace MG.Sonarr.Next.Extensions
{
    public static class HttpStatusCodeExtensions
    {
        #region CACHING
        static readonly Lazy<IReadOnlyDictionary<int, string>> _codes = new(GetLookupCodes);
        static IReadOnlyDictionary<int, string> GetLookupCodes()
        {
            HttpStatusCode[] codes = Enum.GetValues<HttpStatusCode>();
            Dictionary<int, string> dict = new(codes.Length);

            foreach (HttpStatusCode code in codes)
            {
                _ = dict.TryAdd((int)code, ToResponseStringFormat(in code));
            }

            return new ReadOnlyDictionary<int, string>(dict);
        }
        static string ToResponseStringFormat(in HttpStatusCode statusCode)
        {
            string codeStr = statusCode.ToString();

            return string.Create(codeStr.Length + 6, (codeStr, statusCode), (chars, state) =>
            {
                _ = ((int)state.statusCode).TryFormat(chars, out int written, default, Statics.DefaultProvider);
                (stackalloc char[] { ' ', '(' }).CopyToSlice(chars, ref written);

                state.codeStr.CopyToSlice(chars, ref written);
                chars[written] = ')';
            });
        }

        #endregion

        public static string ToResponseString([ValidatedNotNull] this HttpStatusCode statusCode)
        {
            return _codes.Value.TryGetValue((int)statusCode, out string? value)
                ? value
                : statusCode.ToString();
        }

        public static bool TryFormatAsResponse([ValidatedNotNull] this HttpStatusCode statusCode, Span<char> destination, out int written)
        {
            written = 0;
            bool result = false;
            if (_codes.Value.TryGetValue((int)statusCode, out string? value))
            {
                result = value.TryCopyTo(destination);
                if (result)
                {
                    written += value.Length;
                }
            }

            return result;
        }

        private static bool NotDefinedWriteTo(in HttpStatusCode statusCode, Span<char> destination, ref int written)
        {
            ReadOnlySpan<char> sc = statusCode.ToString();
            bool result = sc.TryCopyTo(destination);
            if (result)
            {
                written = sc.Length;
            }

            return result;
        }
    }
}
