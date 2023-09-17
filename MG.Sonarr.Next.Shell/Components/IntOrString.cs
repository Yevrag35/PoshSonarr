using OneOf;

namespace MG.Sonarr.Next.Shell.Components
{
    public readonly struct IntOrString : IEquatable<IntOrString>, IEquatable<int>, IEquatable<string>
    {
        readonly int? _int;
        readonly string? _str;
        readonly bool _isInt;
        readonly bool _isStr;

        [MemberNotNullWhen(true, nameof(_int))]
        [MemberNotNullWhen(false, nameof(_str))]
        public bool IsNumber => _isInt;

        [MemberNotNullWhen(false, nameof(_int))]
        [MemberNotNullWhen(true, nameof(_str))]
        public bool IsString => _isStr;

        public int AsInt => _int.GetValueOrDefault();
        public string AsString => IsString ? _str : string.Empty;

        private IntOrString(in int value)
        {
            _isInt = true;
            _isStr = false;
            _int = value;
            _str = null;
        }
        private IntOrString(string value)
        {
            _isInt = false;
            _isStr = true;
            _int = default;
            _str = value ?? string.Empty;
        }

        public static readonly IntOrString Empty = default;

        public bool Equals(IntOrString other)
        {
            if (IsString && other.IsString)
            {
                return StringComparer.InvariantCultureIgnoreCase.Equals(_str, other._str);
            }
            else if (IsNumber && other.IsNumber)
            {
                return _int.Value == other._int;
            }
            else
            {
                return false;
            }
        }
        public bool Equals(int other)
        {
            return other == _int;
        }
        public bool Equals(string? other)
        {
            return StringComparer.InvariantCultureIgnoreCase.Equals(_str, other);
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is null && !IsNumber && !IsString)
            {
                return true;
            }

            if (obj is IntOrString other)
            {
                return Equals(other);
            }
            else if (obj is int intVal)
            {
                return Equals(intVal);
            }
            else if (obj is string strVal)
            {
                return Equals(strVal);
            }

            return false;
        }
        public override int GetHashCode()
        {
            if (IsNumber)
            {
                return _int.GetHashCode();
            }
            else if (IsString)
            {
                return StringComparer.InvariantCultureIgnoreCase.GetHashCode(_str);
            }
            else
            {
                return base.GetHashCode();
            }
        }
        public static bool TryParse(object? value, out IntOrString result)
        {
            result = Empty;
            if (value is int number)
            {
                result = new(in number);
                return true;
            }
            else if (value is string str)
            {
                result = new(str);
                return true;
            }

            return false;
        }

        public static implicit operator IntOrString(int value)
        {
            return new(in value);
        }
        public static implicit operator IntOrString(string? value)
        {
            return value is not null ? new(value) : Empty;
        }

        public static explicit operator int(IntOrString value) => value._int.GetValueOrDefault();
        public static explicit operator string(IntOrString value) => value.AsString;

        public static bool operator ==(IntOrString x, IntOrString y)
        {
            return x.Equals(y);
        }
        public static bool operator !=(IntOrString x, IntOrString y)
        {
            return !(x == y);
        }
    }
}
