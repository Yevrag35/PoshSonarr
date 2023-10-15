using MG.Sonarr.Next.Attributes;

namespace MG.Sonarr.Next.Extensions
{
    /// <summary>
    /// Custom extension methods for high-performance split operations of <see cref="string"/> 
    /// and <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> instances.
    /// </summary>
    public static class SplittingExtensions
    {
        
        public static int GetIgnoreCaseHashCode(this string str)
        {
            ArgumentNullException.ThrowIfNull(str);
            return StringComparer.InvariantCultureIgnoreCase.GetHashCode(str);
        }

        /// <summary>
        /// A zero-allocation method for enumerating a <see cref="ReadOnlySpan{T}"/> source into sections
        /// split by a given character.
        /// </summary>
        /// <remarks>
        ///     As <see cref="SplitEnumerator"/> is a ref struct, this method cannot be used in
        ///     <see langword="async"/> methods.
        ///     <para>
        ///         An example of utilizing this method looks like:
        ///         <code>foreach (ReadOnlySpan&lt;char&gt; section in "key:value".AsSpan().SpanSplit(':'))</code>
        ///     </para>
        /// </remarks>
        /// <param name="value">The source span to split.</param>
        /// <param name="splitBy">The character value to split the source span on.</param>
        /// <returns>
        ///     A ref struct enumerator that loops through each <see cref="ReadOnlySpan{T}"/> section when
        ///     split by <paramref name="splitBy"/>.
        /// </returns>
        [DebuggerStepThrough]
        public static SplitEnumerator SpanSplit(this ReadOnlySpan<char> value, in char splitBy)
        {
            Guard.IsSpan(value);
            return SpanSplit(value, new ReadOnlySpan<char>(in splitBy));
        }
        /// <summary>
        /// A zero-allocation method for enumerating a <see cref="ReadOnlySpan{T}"/> source into sections
        /// split by a given character.
        /// </summary>
        /// <remarks>
        ///     As <see cref="SplitEnumerator"/> is a ref struct, this method cannot be used in
        ///     <see langword="async"/> methods.
        ///     <para>
        ///         An example of utilizing this method looks like:
        ///         <code>foreach (ReadOnlySpan&lt;char&gt; section in "key : value".AsSpan().SpanSplit(" : "))</code>
        ///     </para>
        /// </remarks>
        /// <param name="value">The source span to split.</param>
        /// <param name="splitBy">A span of <see cref="char"/> values to split on.</param>
        /// <returns>
        ///     A ref struct enumerator that loops through each <see cref="ReadOnlySpan{T}"/> section when
        ///     split by <paramref name="splitBy"/>.
        /// </returns>
        [DebuggerStepThrough]
        public static SplitEnumerator SpanSplit(this ReadOnlySpan<char> value, ReadOnlySpan<char> splitBy)
        {
            Guard.IsSpan(value, splitBy);
            return new SplitEnumerator(value, splitBy);
        }
        /// <summary>
        /// A zero-allocation method for enumerating a <see cref="ReadOnlySpan{T}"/> source into sections
        /// split by 2 choices of <see cref="ReadOnlySpan{T}"/> values.
        /// </summary>
        /// <remarks>
        ///     As <see cref="DoubleSplitEnumerator"/> is a ref struct, this method cannot be used in
        ///     <see langword="async"/> methods.
        ///     <para>
        ///         An example of utilizing this method looks like:
        ///         <code>foreach (ReadOnlySpan&lt;char&gt; section in "key : value".AsSpan().SpanSplit(" : "))</code>
        ///     </para>
        /// </remarks>
        /// <param name="value">The source span to split.</param>
        /// <param name="splitBy1">The first span of <see cref="char"/> values option to split on.</param>
        /// <param name="splitBy2">The second span of <see cref="char"/> values option to split on.
        /// </param>
        /// <returns>
        ///     A ref struct enumerator that loops through each <see cref="ReadOnlySpan{T}"/> section when
        ///     split by <paramref name="splitBy1"/> or <paramref name="splitBy2"/>.
        /// </returns>
        [DebuggerStepThrough]
        public static DoubleSplitEnumerator SpanSplit(this ReadOnlySpan<char> value, ReadOnlySpan<char> splitBy1, ReadOnlySpan<char> splitBy2)
        {
            Guard.IsSpan(value, splitBy1, splitBy2);
            return new DoubleSplitEnumerator(value, splitBy1, splitBy2);
        }
    }
}
