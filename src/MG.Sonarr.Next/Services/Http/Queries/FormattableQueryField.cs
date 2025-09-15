using MG.Sonarr.Next.Extensions.Strings;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Services.Http.Queries;

[StructLayout(LayoutKind.Auto)]
public readonly struct FormattableQueryField : IQueryField
{
    private readonly string? _key;
    private readonly State _value;

    public string Key => _key ?? string.Empty;
    [MemberNotNullWhen(false, nameof(_key))]
    public bool IsDefaultOrEmpty => _key is null;
    public readonly int MaxLength => _value.MaxLength;

    public FormattableQueryField(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        value ??= string.Empty;
        _key = key;
        _value = new(value, value.Length + key.Length + 1);
    }
    public FormattableQueryField(string key, ISpanFormattable value, int maxLength, string? format = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _key = key;
        FormattableObject obj = new(value, format);
        _value = new(obj, maxLength + key.Length + 1);
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        charsWritten = 0;
        if (this.IsDefaultOrEmpty)
        {
            return false;
        }

        _key.CopyToSlice(destination, ref charsWritten);
        destination[charsWritten++] = '=';

        if (_value.TryWrite(destination.Slice(charsWritten), out int written, format, provider))
        {
            charsWritten += written;
            return true;
        }
        
        return false;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        Span<char> chars = stackalloc char[_value.MaxLength];
        if (this.TryFormat(chars, out int charsWritten, format.AsSpan(), formatProvider))
        {
            return new string(chars.Slice(0, charsWritten));
        }
        else
        {
            return string.Empty;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct State
    {
        private readonly object? _value;
        private readonly uint _index;
        private readonly int _maxLength;

        internal uint Index => _index;
        internal int MaxLength => _maxLength;

        internal State(string value, int maxLength)
        {
            _value = value;
            _maxLength = maxLength;
            _index = 1;
        }
        internal State(FormattableObject value, int maxLength)
        {
            _value = value;
            _maxLength = maxLength;
            _index = 2;
        }

        internal unsafe bool TryWrite(Span<char> destination, out int written, ReadOnlySpan<char> format, IFormatProvider? formatProvider)
        {
            written = _index switch
            {
                1 => WriteString(Unsafe.As<string>(_value!), destination),
                2 => WriteFormattable(ref Unsafe.Unbox<FormattableObject>(_value!), destination, format, formatProvider),
                _ => -1,
            };

            return written >= 0;
        }

        private static int WriteString(
            string value,
            Span<char> destination)
        {
            value.CopyTo(destination);
            return value.Length;
        }
        private static int WriteFormattable(
            ref readonly FormattableObject formattable,
            Span<char> destination,
            ReadOnlySpan<char> format,
            IFormatProvider? formatProvider)
        {
            _ = formattable.TryFormat(destination, out int written, format, formatProvider);
            return written;
        }
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct FormattableObject : ISpanFormattable
    {
        internal string? Format { get; }
        internal ISpanFormattable Value { get; }

        internal FormattableObject(ISpanFormattable value, string? format)
        {
            this.Value = value;
            this.Format = format;
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? formatProvider)
        {
            if (!string.IsNullOrWhiteSpace(this.Format))
            {
                format = this.Format;
            }

            return this.Value.TryFormat(destination, out charsWritten, format, formatProvider);
        }
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return this.Value.ToString(format, formatProvider);
        }
    }
}
