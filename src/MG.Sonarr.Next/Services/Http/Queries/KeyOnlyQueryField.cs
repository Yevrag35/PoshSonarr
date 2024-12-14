using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Services.Http.Queries;

[DebuggerStepThrough]
[StructLayout(LayoutKind.Auto)]
public readonly record struct KeyOnlyQueryField : IQueryField
{
    private readonly string? _key;
    private readonly bool _isNotEmpty;

    [MemberNotNullWhen(false, nameof(_key))]
    public bool IsDefaultOrEmpty => !_isNotEmpty;
    public string Key => _key ?? string.Empty;
    public int MaxLength => this.Key.Length;

    public KeyOnlyQueryField(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _key = key;
        _isNotEmpty = true;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return this.Key;
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        bool result = this.Key.AsSpan().TryCopyTo(destination);
        charsWritten = this.Key.Length;
        return result;
    }
}