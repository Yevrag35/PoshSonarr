using OneOf;
using System.Net;

namespace MG.Sonarr.Next.Services.Http
{
    public readonly struct QueryParameter : IComparable<QueryParameter>, IEquatable<QueryParameter>, ISpanFormattable
    {
        readonly string? _key;
        readonly string? _value;
        readonly ISpanFormattable? _formattable;
        readonly int _maxLength;
        readonly bool _isNotEmpty;
        readonly bool _isFormattable;

        public bool IsEmpty => !_isNotEmpty;
        [MemberNotNullWhen(true, nameof(_formattable))]
        public bool IsFormattable => _isFormattable;
        public string Key => _key ?? string.Empty;
        public int MaxLength => _maxLength;

        private QueryParameter(string key)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            _key = key;

            _value = null;
            _formattable = null;
            _isFormattable = false;
            _maxLength = 0;
            _isNotEmpty = true;
        }

        public QueryParameter(string key, OneOf<string?, ISpanFormattable> oneOf, in int oneOfLength)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            _key = key;
            bool isStr = oneOf.TryPickT0(out string? val, out ISpanFormattable? remainder);
            _value = val;
            _formattable = remainder;
            _isFormattable = !isStr;

            _maxLength = key.Length + 1 + oneOfLength;
            _isNotEmpty = true;
        }

        public int CompareTo(QueryParameter other)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(_key, other._key);
        }
        public bool Equals(QueryParameter other)
        {
            return StringComparer.InvariantCultureIgnoreCase.Equals(_key, other._key);
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is QueryParameter other)
            {
                return this.Equals(other);
            }
            else
            {
                return false;
            } 
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(StringComparer.InvariantCultureIgnoreCase.GetHashCode(this.Key));
        }

        public string ToString(string? format, IFormatProvider? provider)
        {
            Span<char> span = stackalloc char[_maxLength];
            _ = this.TryFormat(span, out int written, format, Statics.DefaultProvider);
            return new string(span.Slice(0, written));
        }
        public bool TryFormat(Span<char> span, out int written, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            string key = this.Key;
            key.CopyTo(span);
            written = key.Length;

            span[written++] = '=';
            if (this.IsFormattable && _formattable.TryFormat(span.Slice(written), out int formatWritten, format, provider))
            {
                written += formatWritten;
            }
            else
            {
                string val = _value ?? string.Empty;
                val.CopyTo(span.Slice(written));
                written += val.Length;
            }

            return true;
        }

        public static QueryParameter Create(string key, ISpanFormattable formattable, in int maxLength)
        {
            return new(key, OneOf<string?, ISpanFormattable>.FromT1(formattable), in maxLength);
        }
        public static QueryParameter Create(string key, string? value)
        {
            return new(key, OneOf<string?, ISpanFormattable>.FromT0(value), value?.Length ?? 0);
        }
        public static implicit operator QueryParameter(KeyValuePair<string, string?> kvp)
        {
            return Create(kvp.Key, kvp.Value);
        }
        public static implicit operator QueryParameter(string key)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            return new(key);
        }

        public static bool operator ==(QueryParameter x, QueryParameter y)
        {
            return x.Equals(y);
        }
        public static bool operator !=(QueryParameter x, QueryParameter y)
        {
            return !(x == y);
        }
    }
}
