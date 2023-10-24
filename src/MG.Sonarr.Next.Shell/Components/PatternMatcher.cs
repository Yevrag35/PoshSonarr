using MG.Sonarr.Next.Attributes;

namespace MG.Sonarr.Next.Shell.Components
{
    internal readonly ref struct PatternMatcher
    {
        const char QUESTION = '?';
        const char STAR = '*';

        readonly bool _containsWc;
        readonly bool _isNotEmpty;
        readonly int _length;
        readonly ReadOnlySpan<char> _pattern;

        internal bool ContainsWildcards => _containsWc;
        internal bool IsEmpty => !_isNotEmpty;
        internal int Length => _length;

        internal PatternMatcher(ReadOnlySpan<char> span)
        {
            _pattern = span;
            _isNotEmpty = !span.IsEmpty;
            _length = span.Length;
            _containsWc = ContainsWildcardCharacters(span);
        }

        private static bool AreCharactersEqual(in char x, in char y)
        {
            return char.ToLowerInvariant(x) == char.ToLowerInvariant(y);
        }
        private static bool ContainsWildcardCharacters(ReadOnlySpan<char> pattern)
        {
            Guard.IsSpan(pattern);
            return pattern.IndexOfAny(stackalloc char[] { STAR, QUESTION }) >= 0;
        }
        internal bool IsMatch(ReadOnlySpan<char> input)
        {
            Guard.IsSpan(input);

            int starIndex = -1;
            int iIndex = -1;

            int i = 0;
            int j = 0;

            while (i < input.Length)
            {
                if (j < _pattern.Length && (_pattern[j] == QUESTION || AreCharactersEqual(_pattern[j], input[i])))
                {
                    ++i;
                    ++j;
                }
                else if (j < _pattern.Length && _pattern[j] == STAR)
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

            while (j < _pattern.Length && _pattern[j] == STAR)
            {
                ++j;
            }

            return j == _pattern.Length;
        }
    }
}
