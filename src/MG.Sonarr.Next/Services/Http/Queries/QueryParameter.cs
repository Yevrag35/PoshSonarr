using MG.Sonarr.Next.Extensions;

namespace MG.Sonarr.Next.Services.Http.Queries
{
    public readonly struct QueryParameter : IComparable<QueryParameter>, IEquatable<QueryParameter>, ISpanFormattable, IQueryParameter
    {
        readonly string? _key;
        readonly string? _value;
        readonly ISpanFormattable? _formattable;
        readonly int _maxLength;
        readonly bool _isNotEmpty;
        readonly bool _isFormattable;
        readonly string? _format;

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
            _format = null;
        }

        public QueryParameter(string key, OneOf<string?, ISpanFormattable> oneOf, in int oneOfLength, string? format = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            _key = key;
            bool isStr = oneOf.TryPickT0(out string? val, out ISpanFormattable? remainder);
            _value = val;
            _formattable = remainder;
            _isFormattable = !isStr;

            _maxLength = key.Length + 1 + oneOfLength;
            _isNotEmpty = true;
            _format = format;
        }

        public int CompareTo(QueryParameter other)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(_key, other._key);
        }
        public bool Equals(QueryParameter other)
        {
            return StringComparer.InvariantCultureIgnoreCase.Equals(_key, other._key);
        }
        public bool Equals(IQueryParameter? other)
        {
            return StringComparer.InvariantCultureIgnoreCase.Equals(_key, other?.Key);
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is QueryParameter other && this.Equals(other);
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
            ReadOnlySpan<char> key = this.Key;
            written = key.Length;
            key.CopyTo(span);
            ref char first = ref span[0];
            first = char.ToLower(first);

            if (format.IsEmpty)
            {
                format = _format;
            }

            span[written++] = '=';
            if (this.IsFormattable && _formattable.TryFormat(span.Slice(written), out int formatWritten, format, provider))
            {
                written += formatWritten;
            }
            else
            {
                ReadOnlySpan<char> val = _value;
                val.CopyToSlice(span, ref written);
            }

            return true;
        }

        public static QueryParameter Create(string key, ISpanFormattable formattable, in int maxLength, string? format = null)
        {
            return new QueryParameter(key, OneOf<string?, ISpanFormattable>.FromT1(formattable), in maxLength, format);
        }
        public static IQueryParameter Create(string key, bool value)
        {
            return new QueryBooleanParameter(key, in value);
        }
        public static QueryParameter Create(string key, string? value, string? format = null)
        {
            return new(key, OneOf<string?, ISpanFormattable>.FromT0(value), value?.Length ?? 0, format);
        }
        public static implicit operator QueryParameter(KeyValuePair<string, string?> kvp)
        {
            return Create(kvp.Key, kvp.Value);
        }
        public static explicit operator QueryParameter(string key)
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

    internal readonly struct QueryBooleanParameter : IQueryParameter
    {
        readonly string? _key;
        readonly bool _value;
        readonly int _maxLength;
        readonly bool _isNotEmpty;

        public bool IsEmpty => !_isNotEmpty;
        public string Key => _key ?? string.Empty;
        public int MaxLength => _maxLength;
        public bool Value => _value;

        public QueryBooleanParameter(string key, in bool value)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            _key = key;
            _value = value;
            _maxLength = key.Length + 1 + bool.FalseString.Length;
            _isNotEmpty = true;
        }

        public bool Equals(IQueryParameter? other)
        {
            return StringComparer.InvariantCultureIgnoreCase.Equals(_key, other?.Key);
        }
        public bool Equals(QueryParameter other)
        {
            return StringComparer.InvariantCultureIgnoreCase.Equals(_key, other.Key);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(StringComparer.InvariantCultureIgnoreCase.GetHashCode(this.Key));
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            Span<char> span = stackalloc char[this.MaxLength];
            _ = this.TryFormat(span, out int written, default, null);
            return new string(span.Slice(0, written));
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            ReadOnlySpan<char> key = this.Key;
            key.CopyTo(destination);
            ref char first = ref destination[0];
            first = char.ToLower(first);

            charsWritten = key.Length;

            destination[charsWritten++] = '=';
            
            if (!_value.TryFormat(destination.Slice(charsWritten), out int written))
            {
                charsWritten += written;
                return false;
            }

            ref char c = ref destination[charsWritten];
            c = char.ToLower(c);

            charsWritten += written;
            return true;
        }
    }
}
