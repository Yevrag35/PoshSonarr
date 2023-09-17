namespace MG.Sonarr.Next.Shell.Components
{
    public readonly struct WildcardString : IComparable<WildcardString>, IEquatable<WildcardString>
    {
        readonly WildcardPattern? _pattern;
        readonly string? _str;
        readonly bool _isWild;

        [MemberNotNullWhen(true, nameof(_pattern))]
        public bool IsWildcard => _isWild;
        public string Value => _str ?? string.Empty;

        private WildcardString(WildcardPattern pattern, string value)
        {
            _pattern = pattern;
            _isWild = true;
            _str = value ?? string.Empty;
        }
        private WildcardString(string? value)
        {
            _pattern = null;
            _isWild = false;
            _str = value ?? string.Empty;
        }

        public int CompareTo(WildcardString other)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(_str, other._str);
        }
        public bool Equals(WildcardString other)
        {
            return StringComparer.InvariantCultureIgnoreCase.Equals(_str, other._str);
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is WildcardString ws)
            {
                return this.Equals(ws);
            }
            else if (obj is string str)
            {
                return this.Equals(str);
            }
            else if (obj is null)
            {
                return _str is null;
            }

            return false;
        }
        public override int GetHashCode()
        {
            return _str is not null
                 ? StringComparer.InvariantCultureIgnoreCase.GetHashCode(_str)
                 : base.GetHashCode();
        }
        public bool IsMatch(string? other)
        {
            return this.IsWildcard
                ? _pattern.IsMatch(other)
                : StringComparer.InvariantCultureIgnoreCase.Equals(_str, other);
        }

        public static readonly WildcardString Empty = new(string.Empty);

        public static implicit operator WildcardString(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Empty;
            }
            else if (WildcardPattern.ContainsWildcardCharacters(value))
            {
                return new(new WildcardPattern(value, WildcardOptions.IgnoreCase), value);
            }
            else
            {
                return new(value);
            }
        }
        public static implicit operator string(WildcardString value)
        {
            return value.Value;
        }
    }
}
