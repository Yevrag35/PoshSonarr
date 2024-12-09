using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Unions;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Services.Http.Queries;

[StructLayout(LayoutKind.Auto)]
public readonly record struct FormattableQueryField : IQueryField
{
    private readonly string? _key;
    private readonly Either<string, ISpanFormattable> _value;
    private readonly bool _isNotEmpty;
    private readonly int _maxLength;

    public string? Format { get; }
    public string Key => _key ?? string.Empty;
    [MemberNotNullWhen(false, nameof(_key))]
    public bool IsDefaultOrEmpty => !_isNotEmpty;
    public readonly int MaxLength => _maxLength;

    public FormattableQueryField(string key, string? value)
    {
        value ??= string.Empty;
        this.Format = null;
        _key = key;
        _value = value;
        _maxLength = value.Length + key.Length + 1;
        _isNotEmpty = true;
    }
    public FormattableQueryField(string key, ISpanFormattable value, int maxLength, string? format = null)
    {
        _key = key;
        this.Format = format;
        _value = Either<string, ISpanFormattable>.FromT2(value);
        _maxLength = key.Length + maxLength + 1;
        _isNotEmpty = true;
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        charsWritten = 0;
        if (this.IsDefaultOrEmpty)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(this.Format))
        {
            format = this.Format;
        }

        _key.CopyToSlice(destination, ref charsWritten);
        destination[charsWritten++] = '=';

        if (_value.TryGetT1(out string? strValue, out ISpanFormattable? other))
        {
            bool result = strValue.AsSpan().TryCopyTo(destination.Slice(charsWritten));
            charsWritten += strValue.Length;
            return result;
        }

        return _value.AsT2!.TryCopyToSlice(destination, ref charsWritten, format, provider);
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        Span<char> chars = stackalloc char[_maxLength];
        if (this.TryFormat(chars, out int charsWritten, format.AsSpan(), formatProvider))
        {
            return new string(chars.Slice(0, charsWritten));
        }
        else
        {
            return string.Empty;
        }
    }
}
