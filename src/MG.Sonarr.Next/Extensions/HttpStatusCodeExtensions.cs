using MG.Sonarr.Next.Attributes;
using System.Collections.ObjectModel;
using System.Net;

namespace MG.Sonarr.Next.Extensions
{
    /// <summary>
    /// Custom extension methods for <see cref="HttpStatusCode"/> values.
    /// </summary>
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
            Guard.NotNull(in statusCode);
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

        /// <summary>
        /// Returns the <see cref="string"/> representation of the current 
        /// <see cref="HttpStatusCode"/> value formatted as: <code>int (string)</code>
        /// </summary>
        /// <remarks>
        ///     An example is converting <see cref="HttpStatusCode.BadRequest"/> to:
        ///     <code>400 (BadRequest)</code>
        /// </remarks>
        /// <param name="statusCode">The status code to convert.</param>
        /// <example>200 (OK)</example>
        /// <returns>
        ///     The <see cref="string"/> repsonse form of the <see cref="HttpStatusCode"/> value.
        /// </returns>
        public static string ToResponseString(this HttpStatusCode statusCode)
        {
            Guard.NotNull(in statusCode);
            return _codes.Value.TryGetValue((int)statusCode, out string? value)
                ? value
                : statusCode.ToString();
        }

        /// <summary>
        /// Tries to format the response <see cref="string"/> value of the current 
        /// <see cref="HttpStatusCode"/> instance into the 
        /// provided span of characters.
        /// </summary>
        /// /// <remarks>
        ///     An example is writting <see cref="HttpStatusCode.BadRequest"/> as:
        ///     <code>400 (BadRequest)</code>
        /// </remarks>
        /// <param name="statusCode">The status code to format.</param>
        /// <param name="destination">
        ///     The span in which to write this value formatted as a span of characters.
        /// </param>
        /// <param name="charsWritten">
        ///     When this method returns, contains the number of characters that were written in
        ///     <paramref name="destination"/>
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the formatting was successful; otherwise,
        ///     <see langword="false"/>.
        /// </returns>
        public static bool TryFormatAsResponse(this HttpStatusCode statusCode, Span<char> destination, out int charsWritten)
        {
            Guard.NotNull(in statusCode);
            Guard.IsSpan(destination);

            charsWritten = 0;
            bool result = false;
            if (_codes.Value.TryGetValue((int)statusCode, out string? value))
            {
                result = value.TryCopyTo(destination);
                if (result)
                {
                    charsWritten += value.Length;
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
