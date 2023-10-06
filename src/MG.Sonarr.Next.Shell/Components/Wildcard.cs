using MG.Sonarr.Next.Services.Extensions;
using System.Collections;

namespace MG.Sonarr.Next.Shell.Components
{
    /// <summary>
    /// A read-only <see cref="string"/> that can be used for checking equality and pattern matching based on traditional wildcard characters.
    /// </summary>
    public readonly struct Wildcard : IEquatable<Wildcard>, IEquatable<string>, IEnumerable<char>, ISpanFormattable, ISpanParsable<Wildcard>
    {
        const char QUESTION = '?';
        const char STAR = '*';

        readonly bool _containsWildcards;
        readonly int _length;
        readonly bool _isNotEmpty;
        readonly string? _pattern;

        /// <summary>
        /// Gets the <see cref="char"/> object at the specified position in the current <see cref="Wildcard"/>
        /// object.
        /// </summary>
        /// <param name="index">The position in the current string.</param>
        /// <returns>The char object at the specified index.</returns>
        public char this[int index] => _pattern?[index] ?? default;

        /// <summary>
        /// Indicates whether the <see cref="Wildcard"/> contains any wildcard characters ('?' or '*') in
        /// the string.
        /// </summary>
        /// <remarks>
        ///     If <see langword="false"/>, during the <see cref="IsMatch(ReadOnlySpan{char})"/> and
        ///     <see cref="IsMatch(string?)"/> method executions, only strict <see cref="char"/> equality will
        ///     be checked.
        /// </remarks>
        /// <returns>
        ///     <see langword="true"/> if the string contains at least 1 wildcard character; otherwise,
        ///     <see langword="false"/>.
        /// </returns>
        [MemberNotNullWhen(true, nameof(_pattern))]
        public bool ContainsWildcards => _containsWildcards;

        /// <summary>
        /// Indicates whether the <see cref="Wildcard"/> object is equal to <see cref="string.Empty"/>.
        /// </summary>
        [MemberNotNullWhen(false, nameof(_pattern))]
        public bool IsEmpty => !_isNotEmpty;
        /// <summary>
        /// Gets the number of characters in the current <see cref="Wildcard"/> object.
        /// </summary>
        public int Length => _length;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wildcard"/> struct using the 
        /// specified <see cref="string"/>.
        /// </summary>
        /// <param name="pattern">The string pattern to use.</param>
        public Wildcard(string? pattern)
        {
            bool notEmpty = !string.IsNullOrEmpty(pattern);
            _isNotEmpty = notEmpty;
            pattern ??= string.Empty;
            _length = pattern.Length;
            _containsWildcards = notEmpty && ContainsWildcardCharacters(pattern);
            _pattern = pattern;
        }

        private static bool AreCharactersEqual(in char x, in char y)
        {
            return char.ToLowerInvariant(x) == char.ToLowerInvariant(y);
        }
        /// <summary>
        /// Creates a new read-only span over the <see cref="Wildcard"/> object.
        /// </summary>
        /// <returns>The read-only span representation of the <see cref="Wildcard"/>.</returns>
        public ReadOnlySpan<char> AsSpan()
        {
            return _pattern.AsSpan();
        }
        /// <summary>
        /// Creates a new read-only span over a portion of the <see cref="Wildcard"/> object from a specified
        /// position to the end of the string.
        /// </summary>
        /// <param name="start">The zero-based index at which to begin this slice.</param>
        /// <returns>The read-only span representation of the <see cref="Wildcard"/>.</returns>
        public ReadOnlySpan<char> AsSpan(int start)
        {
            return _pattern.AsSpan(start);
        }
        /// <summary>
        /// Creates a new read-only span over a portion of the <see cref="Wildcard"/> object from a specified
        /// position for a specified number of characters.
        /// </summary>
        /// <param name="start">The zero-based index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice.</param>
        /// <returns>The read-only span representation of the <see cref="Wildcard"/>.</returns>
        public ReadOnlySpan<char> AsSpan(int start, int length)
        {
            return _pattern.AsSpan(start, length);
        }
        private static bool ContainsWildcardCharacters(ReadOnlySpan<char> pattern)
        {
            return pattern.IndexOfAny(stackalloc char[] { STAR, QUESTION }) >= 0;
        }

        public bool Equals(Wildcard other)
        {
            return StringComparer.InvariantCultureIgnoreCase.Equals(_pattern, other._pattern);
        }
        public bool Equals(string? other)
        {
            return StringComparer.InvariantCultureIgnoreCase.Equals(_pattern, other);
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Wildcard other)
            {
                return this.Equals(other);
            }
            else if (obj is string s)
            {
                return this.Equals(new (s));
            }

            return false;
        }
        public IEnumerator<char> GetEnumerator()
        {
            string pat = _pattern ?? string.Empty;
            return pat.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        public override int GetHashCode()
        {
            if (this.IsEmpty)
            {
                return 0;
            }

            return StringComparer.InvariantCultureIgnoreCase.GetHashCode(_pattern);
        }

        /// <summary>
        /// Determines if the input <see cref="string"/> object matches the current 
        /// <see cref="Wildcard"/> object based on traditional wildcard pattern rules.
        /// </summary>
        /// <remarks>
        ///     Traditional pattern matching includes:
        ///     <para>
        ///         <code>* - 0 or more of any character.<br/>
        ///     ? - exactly 1 of any character.</code>
        ///     </para>
        /// </remarks>
        /// <param name="input">The string to pattern match.</param>
        /// <returns>
        ///     <see langword="true"/> if <paramref name="input"/> matches the <see cref="Wildcard"/> pattern;
        ///     otherwise, <see langword="false"/>.
        /// </returns>
        public bool IsMatch(string? input)
        {
            return this.IsMatch(input.AsSpan());
        }
        /// <summary>
        /// Determines if the specified <see cref="char"/> span matches the current 
        /// <see cref="Wildcard"/> object based on traditional wildcard pattern rules.
        /// </summary>
        /// <remarks>
        ///     Traditional pattern matching includes:
        ///     <para>
        ///         <code>* - 0 or more of any character.<br/>
        ///     ? - exactly 1 of any character.</code>
        ///     </para>
        /// </remarks>
        /// <param name="input">The string to pattern match.</param>
        /// <returns>
        ///     <see langword="true"/> if <paramref name="input"/> matches the <see cref="Wildcard"/> pattern;
        ///     otherwise, <see langword="false"/>.
        /// </returns>
        public bool IsMatch(ReadOnlySpan<char> input)
        {
            return _containsWildcards
                ? IsMatch(_pattern.AsSpan(), input)
                : _pattern.AsSpan().Equals(input, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsMatch(ReadOnlySpan<char> pattern, ReadOnlySpan<char> input)
        {
            int starIndex = -1;
            int iIndex = -1;

            int i = 0;
            int j = 0;

            while (i < input.Length)
            {
                if (j < pattern.Length && (pattern[j] == QUESTION || AreCharactersEqual(pattern[j], input[i])))
                {
                    ++i;
                    ++j;
                }
                else if (j < pattern.Length && pattern[j] == STAR)
                {
                    starIndex = j;
                    iIndex = i;
                    ++j;
                }
                else if (starIndex == -1)
                {
                    return false;
                }
                else
                {
                    j = starIndex + 1;
                    i = iIndex + 1;
                    iIndex++;
                }
            }

            while (j < pattern.Length && pattern[j] == STAR)
            {
                ++j;
            }

            return j == pattern.Length;
        }

        #region FORMATTABLE
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            charsWritten = 0;
            return this.AsSpan().TryCopyToSlice(destination, ref charsWritten);
        }
        /// <summary>
        /// Returns the underlying <see cref="string"/> instance of this <see cref="Wildcard"/> object; no
        /// actual conversion is performed.
        /// </summary>
        /// <returns>The underlying <see cref="string"/>.</returns>
        public override string ToString()
        {
            return !this.IsEmpty ? _pattern : string.Empty;
        }
        string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
        {
            return this.ToString();
        }

        #endregion

        #region PARSABLE
        public static Wildcard Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return !s.IsEmpty
                ? new(new string(s))
                : Empty;
        }
        public static Wildcard Parse(string? s, IFormatProvider? provider)
        {
            return s;
        }
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Wildcard result)
        {
            result = Parse(s, provider);
            return true;
        }
        public static bool TryParse(string? s, IFormatProvider? provider, out Wildcard result)
        {
            result = Parse(s, provider);
            return true;
        }

        #endregion

        /// <summary>
        /// A <see langword="static"/>, read-only instance of <see cref="Wildcard"/> representing an 
        /// empty pattern.
        /// </summary>
        public static readonly Wildcard Empty = new(string.Empty);
        public static implicit operator Wildcard(string? pattern)
        {
            return !string.IsNullOrEmpty(pattern) ? new Wildcard(pattern) : Empty;
        }
        public static explicit operator string(Wildcard pattern)
        {
            return pattern.ToString();
        }
        public static implicit operator ReadOnlySpan<char>(Wildcard pattern)
        {
            return pattern.AsSpan();
        }

        public static bool operator ==(Wildcard x, Wildcard y)
        {
            return x.Equals(y);
        }
        public static bool operator !=(Wildcard x, Wildcard y)
        {
            return !(x == y);
        }
    }
}
