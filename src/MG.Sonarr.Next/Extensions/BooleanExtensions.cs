using MG.Sonarr.Next.Attributes;

namespace MG.Sonarr.Next.Extensions
{
    /// <summary>
    /// Custom extension methods for <see cref="bool"/> values.
    /// </summary>
    public static class BooleanExtensions
    {
        /// <summary>
        /// Returns the number of characters the <see cref="bool"/> value's <see cref="string"/>
        /// representation would be.
        /// </summary>
        /// <param name="value">The bool value.</param>
        /// <returns>The number of characters in the <see cref="bool"/> value's length.</returns>
        public static int GetLength([ValidatedNotNull] this bool value)
        {
            return value ? bool.TrueString.Length : bool.FalseString.Length;
        }
        /// <summary>
        /// Tries to format the value of the current <see cref="bool"/> instance into the 
        /// provided span of characters.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="destination">
        ///     The span in which to write this instance's value formatted as a span of characters.
        /// </param>
        /// <param name="lowerCase">
        ///     Indicates whether this instance's value should be copied as lowercase characters.
        /// </param>
        /// <param name="charsWritten">
        ///     When this method returns, contains the number of characters that were written in
        ///     <paramref name="destination"/>
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the formatting was successful; otherwise,
        ///     <see langword="false"/>.
        /// </returns>
        public static bool TryFormat([ValidatedNotNull] this bool value, [ValidatedNotNull] Span<char> destination, bool lowerCase, out int charsWritten)
        {
            if (lowerCase)
            {
                ReadOnlySpan<char> boolStr = value.ToString();
                Span<char> scratch = stackalloc char[destination.Length];
                charsWritten = boolStr.ToLower(scratch, Statics.DefaultCulture);
                return scratch.Slice(0, charsWritten).TryCopyTo(destination);
            }
            else
            {
                return value.TryFormat(destination, out charsWritten);
            }
        }
    }
}
