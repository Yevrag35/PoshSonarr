using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Strings
{
    public readonly partial struct Wildcard
    {
        [StructLayout(LayoutKind.Sequential)]
        private readonly struct State
        {
            private readonly WildcardMatchType _type;
            private readonly ushort _length;

            public int Length => _length;
            public WildcardMatchType Type => _type;

            internal State(int length, WildcardMatchType type)
            {
                _length = (ushort)length;
                _type = type;
            }

            public bool Equals(State other)
            {
                return _length == other._length && this.Type == other.Type;
            }
        }
    }
}
