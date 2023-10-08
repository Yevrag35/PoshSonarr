using MG.Sonarr.Next.Attributes;

namespace MG.Sonarr.Next.Extensions
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
        public static void CopyToSlice(this string? value, Span<char> span, scoped ref int position)
        {
            Guard.IsSpan(span);
            CopyToSlice(spanValue: Guard.AsSpan(value), span, ref position);
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
        public static void CopyToSlice(this ReadOnlySpan<char> spanValue, Span<char> span, scoped ref int position)
        {
            Guard.IsSpan(spanValue, span);

            if (!spanValue.IsEmpty && spanValue.TryCopyTo(span.Slice(position)))
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
        public static void CopyToSlice(this Span<char> writtableSpan, Span<char> span, scoped ref int position)
        {
            Guard.IsSpan(writtableSpan, span);
            CopyToSlice(spanValue: writtableSpan, span, ref position);
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
        public static bool TryCopyToSlice(this ReadOnlySpan<char> spanValue, Span<char> span, scoped ref int position)
        {
            Guard.IsSpan(spanValue, span);
            bool result = false;

            if (!spanValue.IsEmpty && spanValue.TryCopyTo(span.Slice(position)))
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
        public static bool TryCopyToSlice(this Span<char> writtableSpan, Span<char> span, scoped ref int position)
        {
            Guard.IsSpan(writtableSpan, span);
            return TryCopyToSlice(spanValue: writtableSpan, span, ref position);
        }
        /// <summary>
        /// Determines whether the beginning of the <paramref name="span"/> matches the specified <paramref name="value"/> when compared ignoring case.
        /// </summary>
        /// <param name="span">The source span.</param>
        /// <param name="value">The character to compare to the beginning of the source span.</param>
        /// <returns>
        ///     <see langword="true"/> if <paramref name="value"/> matches the beginning of 
        ///     <paramref name="span"/>; otherwise, <see langword="false"/>.
        /// </returns>
        [DebuggerStepThrough]
        public static bool StartsWith(this ReadOnlySpan<char> span, in char value)
        {
            Guard.IsSpan(span);

            return span.StartsWith(
                new ReadOnlySpan<char>(in value),
                StringComparison.InvariantCultureIgnoreCase);
        }
        /// <summary>
        /// Determines whether the beginning of the <paramref name="span"/> matches the specified <paramref name="value"/> when compared using the specified 
        /// <paramref name="comparisonType"/> option.
        /// </summary>
        /// <param name="span">The source span.</param>
        /// <param name="value">The character to compare to the beginning of the source span.</param>
        /// <param name="comparisonType">
        ///     One of the enumeration values that determines how the 
        ///     <paramref name="span"/> and <paramref name="value"/> are compared.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if <paramref name="value"/> matches the beginning of 
        ///     <paramref name="span"/>; otherwise, <see langword="false"/>.
        /// </returns>
        [DebuggerStepThrough]
        public static bool StartsWith(this ReadOnlySpan<char> span, in char value, StringComparison comparisonType)
        {
            Guard.NotNull(comparisonType);
            Guard.IsSpan(span);

            return span.StartsWith(
                new ReadOnlySpan<char>(in value),
                comparisonType);
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
        public static bool StartsWith(this Span<char> span, in char value)
        {
            Guard.IsSpan(span);
            return span.StartsWith(new ReadOnlySpan<char>(in value));
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
        public static bool StartsWith(this Span<char> span, in char value, bool ignoreCase)
        {
            Guard.IsSpan(span);

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
