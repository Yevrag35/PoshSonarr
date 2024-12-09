using MG.Sonarr.Next.Extensions.Strings;

namespace MG.Sonarr.Next.Extensions.Strings
{
    /// <summary>
    /// Custom extension methods for <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/> instances
    /// of type <see cref="char"/>.
    /// </summary>
    public static class CharSpanExtensions
    {
        /// <summary>
        /// Copies the characters of this <see cref="string"/> instance into a destination
        /// <see cref="Span{T}"/> and advances the given ref <see cref="int"/> the 
        /// <see cref="string.Length"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="span">The span to copy items into.</param>
        /// <param name="position">
        ///     The ref <see cref="int"/> to add the number of the characters to if copying was
        ///     successful.
        /// </param>
        [DebuggerStepThrough]
        public static void CopyToSlice(this string? value, Span<char> span, ref int position)
        {
            CopyToSlice(spanValue: value, span, ref position);
        }
        /// <summary>
        /// Copies the contents of this <see cref="ReadOnlySpan{T}"/> into a destination 
        /// <see cref="Span{T}"/> and advances the given ref <see cref="int"/> the number 
        /// of characters that were copied.
        /// </summary>
        /// <param name="spanValue">The source read-only span whose characters are copied.</param>
        /// <param name="span">The span to copy items into.</param>
        /// <param name="position">
        ///     The ref <see cref="int"/> to add the number of the characters to if copying was
        ///     successful.
        /// </param>
        public static void CopyToSlice(this ReadOnlySpan<char> spanValue, Span<char> span, ref int position)
        {
            if (spanValue.TryCopyTo(span.Slice(position)))
            {
                position += spanValue.Length;
            }
        }
        /// <summary>
        /// Copies the contents of this <see cref="Span{T}"/> into a destination 
        /// <see cref="Span{T}"/> and advances the given ref <see cref="int"/> the number 
        /// of characters that were copied.
        /// </summary>
        /// <param name="writtableSpan">The source span whose characters are copied.</param>
        /// <param name="span">The span to copy items into.</param>
        /// <param name="position">
        ///     The ref <see cref="int"/> to add the number of the characters to if copying was
        ///     successful.
        /// </param>
        [DebuggerStepThrough]
        public static void CopyToSlice(this Span<char> writtableSpan, Span<char> span, ref int position)
        {
            CopyToSlice(spanValue: writtableSpan, span, ref position);
        }

        public static void CopyToSlice<T>(this T value, Span<char> destination, ref int position, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
            where T : ISpanFormattable
        {
            if (value.TryFormat(destination.Slice(position), out int written, format, provider))
            {
                position += written;
            }
        }

        public static bool EnclosedIn([NotNullWhen(true)] this string? value, char openingChar, char closingChar)
        {
            return EnclosedInCore(value.AsSpan(), in openingChar, in closingChar);
        }
        public static bool EnclosedIn(this ReadOnlySpan<char> readOnlySpan, char openingChar, char closingChar)
        {
            return EnclosedInCore(readOnlySpan, in openingChar, in closingChar);
        }
        public static bool EnclosedIn(this Span<char> span, char openingChar, char closingChar)
        {
            return EnclosedInCore(span, in openingChar, in closingChar);
        }

        /// <summary>
        /// Attemps to copy the contents of this <see cref="ReadOnlySpan{T}"/> into a 
        /// <see cref="Span{T}"/> and advancing the given ref <see cref="int"/> the number 
        /// of characters that were copied, returning a value indicating whether or not the operation
        /// succeeded.
        /// </summary>
        /// <param name="spanValue">The source read-only span whose characters are copied.</param>
        /// <param name="span">The target of the copy operation.</param>
        /// <param name="position">
        ///     The ref <see cref="int"/> to add the number of the characters to if copying was
        ///     successful.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the copying operation was successful; otherwise
        ///     <see langword="false"/>.
        /// </returns>
        public static bool TryCopyToSlice(this ReadOnlySpan<char> spanValue, Span<char> span, ref int position)
        {
            bool result = false;

            if (spanValue.TryCopyTo(span.Slice(position)))
            {
                position += spanValue.Length;
                result = true;
            }

            return result;
        }
        /// <summary>
        /// Attemps to copy the contents of this <see cref="Span{T}"/> into a destination
        /// <see cref="Span{T}"/> and advancing the given ref <see cref="int"/> the number 
        /// of characters that were copied, returning a value indicating whether or not the operation
        /// succeeded.
        /// </summary>
        /// <param name="writtableSpan">The source span whose characters are copied.</param>
        /// <param name="span">The target of the copy operation.</param>
        /// <param name="position">
        ///     The ref <see cref="int"/> to add the number of the characters to if copying was
        ///     successful.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the copying operation was successful; otherwise
        ///     <see langword="false"/>.
        /// </returns>
        [DebuggerStepThrough]
        public static bool TryCopyToSlice(this Span<char> writtableSpan, Span<char> span, ref int position)
        {
            return TryCopyToSlice(spanValue: writtableSpan, span, ref position);
        }

        public static bool TryCopyToSlice<T>(this T value, Span<char> destination, ref int position, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
            where T : ISpanFormattable
        {
            bool result = false;
            if (value.TryFormat(destination.Slice(position), out int written, format, provider))
            {
                position += written;
                result = true;
            }

            return result;
        }
        /// <summary>
        /// Determines whether the beginning of the <paramref name="readOnlySpan"/> matches the specified <paramref name="value"/> when compared ignoring case.
        /// </summary>
        /// <param name="readOnlySpan">The source span.</param>
        /// <param name="value">The character to compare to the beginning of the source span.</param>
        /// <returns>
        ///     <see langword="true"/> if <paramref name="value"/> matches the beginning of 
        ///     <paramref name="readOnlySpan"/>; otherwise, <see langword="false"/>.
        /// </returns>
        [DebuggerStepThrough]
        public static bool StartsWith(this ReadOnlySpan<char> readOnlySpan, char value)
        {
            return FirstCharEquals(readOnlySpan, in value);
        }
        /// <summary>
        /// Determines whether the beginning of the <paramref name="readOnlySpan"/> matches the specified <paramref name="value"/> when compared using the specified 
        /// <paramref name="comparisonType"/> option.
        /// </summary>
        /// <param name="readOnlySpan">The source span.</param>
        /// <param name="value">The character to compare to the beginning of the source span.</param>
        /// <param name="comparisonType">
        ///     One of the enumeration values that determines how the 
        ///     <paramref name="readOnlySpan"/> and <paramref name="value"/> are compared.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if <paramref name="value"/> matches the beginning of 
        ///     <paramref name="readOnlySpan"/>; otherwise, <see langword="false"/>.
        /// </returns>
        [DebuggerStepThrough]
        public static bool StartsWith(this ReadOnlySpan<char> readOnlySpan, char value, StringComparison comparisonType)
        {
            return readOnlySpan.StartsWith(new ReadOnlySpan<char>(in value), comparisonType);
        }
        /// <summary>
        /// Determines whether the specified sequence appears at the start of the span.
        /// </summary>
        /// <param name="span">The source span.</param>
        /// <param name="value">The character to compare to the beginning of the source span.</param>
        /// <returns>
        ///     <see langword="true"/> if <paramref name="value"/> matches the beginning of 
        ///     <paramref name="span"/>; otherwise, <see langword="false"/>.
        /// </returns>
        [DebuggerStepThrough]
        public static bool StartsWith(this Span<char> span, char value)
        {
            return FirstCharEquals(span, in value);
        }
        /// <summary>
        /// Determines whether the specified sequence appears at the start of the span.
        /// </summary>
        /// <param name="span">The source span.</param>
        /// <param name="value">The character to compare to the beginning of the source span.</param>
        /// <param name="ignoreCase">Indicates the comparison should ignore casing rules.</param>
        /// <returns>
        ///     <see langword="true"/> if <paramref name="value"/> matches the beginning of 
        ///     <paramref name="span"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool StartsWith(this Span<char> span, char value, StringComparison comparisonType)
        {
            return comparisonType switch
            {
                StringComparison.Ordinal => FirstCharEquals(span, in value),
                _ => ((ReadOnlySpan<char>)span).StartsWith(new ReadOnlySpan<char>(in value), comparisonType),
            };
        }

        #region PRIVATE METHODS
        private static bool EnclosedInCore(ReadOnlySpan<char> readOnlySpan, in char opening, in char closing)
        {
            if (readOnlySpan.Length < 2)
            {
                return false;
            }

            ref readonly char first = ref readOnlySpan[0];
            return first == opening && closing == readOnlySpan[^1];
        }

        private static bool FirstCharEquals(ReadOnlySpan<char> span, in char value)
        {
            bool result = false;
            if (!span.IsEmpty)
            {
                ref readonly char c = ref span[0];
                result = c == value;
            }

            return result;
        }

        #endregion
    }
}