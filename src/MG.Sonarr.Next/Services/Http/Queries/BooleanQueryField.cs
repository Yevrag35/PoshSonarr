using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Unions;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Services.Http.Queries;

[StructLayout(LayoutKind.Auto)]
[CollectionBuilder(typeof(BooleanQueryField), nameof(CreateFromSpan))]
public readonly struct BooleanQueryField : IQueryField, IEnumerable<Either<string, bool>>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    static readonly string s_True = bool.TrueString.ToLower();
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    static readonly string s_False = bool.FalseString.ToLower();

    private readonly string? _key;
    private readonly bool _value;
    private readonly bool _isNotEmpty;
    private readonly int _maxLength;

    public string Key => _key ?? string.Empty;
    [MemberNotNullWhen(false, nameof(_key))]
    public bool IsDefaultOrEmpty => !_isNotEmpty;
    public readonly int MaxLength => _maxLength;

    public BooleanQueryField(string key, bool value)
    {
        _key = key;
        _value = value;
        _maxLength = key.Length + 1 + s_False.Length;
        _isNotEmpty = true;
    }

    [DebuggerStepThrough]
    IEnumerator<Either<string, bool>> IEnumerable<Either<string, bool>>.GetEnumerator()
    {
        yield return Either<string, bool>.FromT1(this.Key);
        yield return Either<string, bool>.FromT2(_value);
    }
    [DebuggerStepThrough]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<Either<string, bool>>)this).GetEnumerator();
    }

    public static BooleanQueryField CreateTrue(string key)
    {
        return new BooleanQueryField(key, value: true);
    }
    public static BooleanQueryField CreateFromSpan(params ReadOnlySpan<Either<string, bool>> values)
    {
        if (values.Length != 2)
        {
            return default;
        }

        ref readonly var first = ref values[0];
        ref readonly var second = ref values[1];
        if (first.IsSameType(in second))
        {
            return default;
        }

        return first.Match(second,
            static (key, other) => new BooleanQueryField(key, other.AsT2),
            static (boolean, other) => new BooleanQueryField(other.AsT1!, boolean));
    }

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
    {
        Span<char> chars = stackalloc char[this.MaxLength];
        if (!this.TryFormat(chars, out int charsWritten))
        {
            return string.Empty;
        }

        return new string(chars.Slice(0, charsWritten));
    }
    public bool TryFormat(Span<char> destination, out int charsWritten)
    {
        charsWritten = 0;
        if (this.IsDefaultOrEmpty)
        {
            return false;
        }

        if (!_key.AsSpan().TryCopyToSlice(destination, ref charsWritten))
        {
            return false;
        }

        destination[charsWritten++] = '=';

        string value = _value ? s_True : s_False;
        return value.AsSpan().TryCopyToSlice(destination, ref charsWritten);
    }
    [DebuggerStepThrough]
    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return this.TryFormat(destination, out charsWritten);
    }
}
