namespace MG.Sonarr.Next.Extensions.Strings
{
    [DebuggerStepThrough]
    [StructLayout(LayoutKind.Auto)]
    public ref struct SplitEnumerator
    {
        ReadOnlySpan<char> _str;
        readonly ReadOnlySpan<char> _splitBy;

        public SplitEntry Current { get; private set; }

        public SplitEnumerator(ReadOnlySpan<char> str, ReadOnlySpan<char> splitBy)
        {
            _str = str;
            _splitBy = splitBy;
        }

        public readonly SplitEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            ReadOnlySpan<char> span = _str;
            if (span.Length <= 0)
            {
                return false;
            }

            int index = span.IndexOf(_splitBy);
            if (index < 0)
            {
                _str = ReadOnlySpan<char>.Empty;
                this.Current = new SplitEntry(span, _splitBy);
                return true;
            }

            this.Current = new SplitEntry(span.Slice(0, index), span.Slice(index, _splitBy.Length));
            _str = span.Slice(index + _splitBy.Length);
            return true;
        }
    }

    /// <summary>
    /// Represents a segment of characters and its associated separator, typically produced when splitting a string or
    /// character span.
    /// </summary>
    /// <remarks>A SplitEntry provides access to both the content segment and the separator which follows it,
    /// enabling efficient enumeration of split results without allocating new strings. This struct is a ref struct and
    /// can only be used on the stack; it cannot be stored in fields of classes or boxed.</remarks>
    [DebuggerStepThrough]
    [StructLayout(LayoutKind.Auto)]
    public readonly ref struct SplitEntry
    {
        /// <summary>
        /// Gets the segment of characters that were separated.
        /// </summary>
        public ReadOnlySpan<char> Chars { get; }
        /// <summary>
        /// Gets the separator that follows the segment of characters.
        /// </summary>
        public ReadOnlySpan<char> Separator { get; }

        /// <summary>
        /// Initializes a new instance of the SplitEntry class with the specified character span and separator.
        /// </summary>
        /// <param name="chars">The span of characters to be split. This value represents the input sequence.</param>
        /// <param name="separator">The separator span that delimits segments within the input sequence. This value indicates where the splits occurred.</param>
        public SplitEntry(ReadOnlySpan<char> chars, ReadOnlySpan<char> separator)
        {
            this.Chars = chars;
            this.Separator = separator;
        }

        /// <summary>
        /// Deconstructs the current instance into its character span and separator span.
        /// </summary>
        /// <param name="chars">When this method returns, contains a read-only span of characters representing the main content of the
        /// instance.</param>
        /// <param name="separator">When this method returns, contains a read-only span of characters representing the separator associated with
        /// the instance.</param>
        public void Deconstruct(out ReadOnlySpan<char> chars, out ReadOnlySpan<char> separator)
        {
            chars = this.Chars;
            separator = this.Separator;
        }
        /// <summary>
        /// Implicitly converts a <see cref="SplitEntry"/> instance to a <see cref="ReadOnlySpan{char}"/> representing
        /// the entry's character data.
        /// </summary>
        public static implicit operator ReadOnlySpan<char>(SplitEntry entry)
        {
            return entry.Chars;
        }
    }

    /// <summary>
    /// Enumerates substrings of a character span that are separated by either of two specified delimiters.
    /// </summary>
    /// <remarks>This enumerator splits the input span using the first delimiter if found; otherwise, it uses
    /// the second delimiter. If neither delimiter is present, the remaining span is returned as a single entry. The
    /// enumeration yields each substring along with the delimiter that separated it, if any. This type is a ref struct
    /// and can only be used on the stack. It is typically used in a foreach loop or by manually advancing with
    /// MoveNext().</remarks>
    [DebuggerStepThrough]
    [StructLayout(LayoutKind.Auto)]
    public ref struct DoubleSplitEnumerator
    {
        private ReadOnlySpan<char> _str;
        private readonly ReadOnlySpan<char> _splitBy1;
        private readonly ReadOnlySpan<char> _splitBy2;
        private SplitEntry _current;

        /// <summary>
        /// Gets the current <see cref="SplitEntry"/> in the enumeration, which includes the substring and its preceding separator.
        /// </summary>
        public readonly SplitEntry Current => _current;

        public DoubleSplitEnumerator(ReadOnlySpan<char> str, ReadOnlySpan<char> splitBy1, ReadOnlySpan<char> splitBy2)
        {
            _str = str;
            _splitBy1 = splitBy1;
            _splitBy2 = splitBy2;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection of split double values.
        /// </summary>
        /// <returns>A <see cref="DoubleSplitEnumerator"/> that can be used to iterate through the split double values.</returns>
        public readonly DoubleSplitEnumerator GetEnumerator() => this;

        /// <summary>
        /// Advances the enumerator to the next split entry in the source string.
        /// </summary>
        /// <remarks>This method updates the current entry to the next segment split by the specified
        /// delimiters. After the method returns false, the enumerator is positioned after the last entry and cannot be
        /// advanced further.</remarks>
        /// <returns><see langword="true"/> if the enumerator was successfully advanced to the next entry; otherwise, <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            ReadOnlySpan<char> span = _str;
            if (span.Length <= 0)
            {
                return false;
            }

            int index = span.IndexOf(_splitBy1);
            if (index > -1)
            {
                _current = new SplitEntry(span.Slice(0, index), span.Slice(index, _splitBy1.Length));

                _str = span[(index + _splitBy1.Length)..];
                return true;
            }

            index = span.IndexOf(_splitBy2);
            if (index > -1)
            {
                _current = new SplitEntry(span.Slice(0, index), span.Slice(index, _splitBy2.Length));
                _str = span.Slice(index + _splitBy2.Length);
                return true;
            }

            _str = [];
            _current = new SplitEntry(span, default);
            return true;
        }
    }
}
