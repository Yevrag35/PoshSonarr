using MG.Sonarr.Next.Extensions.Strings;
using System.Collections;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Services.Http.Queries;

[DebuggerDisplay(@"\{Count = {Count}, MaxLength = {MaxLength}\}")]
public sealed class QueryCol : IReadOnlyList<IQueryField>, ISpanFormattable
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    static readonly string s_True = bool.TrueString.ToLower();
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    static readonly string s_False = bool.FalseString.ToLower();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int _maxLength;

    private readonly List<IQueryField> _fields;

    public IQueryField this[int index] => _fields[index];

    public int Count => _fields.Count;
    public int MaxLength => _maxLength;

    [DebuggerStepThrough]
    public QueryCol()
    {
        _fields = [];
    }

    [DebuggerStepThrough]
    public void Add(string key, bool value)
    {
        this.Add(key, value ? s_True : s_False);
    }
    [DebuggerStepThrough]
    public void Add(string key, int value)
    {
        this.Add(key, value, LengthConstants.INT_MAX);
    }
    public void Add(string key, string? value)
    {
        IQueryField field = string.IsNullOrWhiteSpace(value)
            ? new KeyOnlyQueryField(key)
            : new FormattableQueryField(key, value);

        this.Add(field);
    }
    public void Add(string key, ISpanFormattable value, int maxLength, string? format = null)
    {
        FormattableQueryField field = new(key, value, maxLength, format);
        this.Add(field);
    }
    public void Add(IQueryField field)
    {
        _fields.Add(field);
        _maxLength += field.MaxLength;
    }
    [DebuggerStepThrough]
    public void Clear()
    {
        _fields.Clear();
        _maxLength = 0;
    }
    public bool Remove(string key)
    {
        if (this.Count == 0)
        {
            return false;
        }

        bool result = false;
        int index = _fields.FindIndex(f => f.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            _maxLength -= _fields[index].MaxLength;
            _fields.RemoveAt(index);
            result = true;
        }

        return result;
    }
    public void RemoveAll(string key)
    {
        while (this.Count > 0 && this.Remove(key))
        {
        }
    }

    public bool TryFormat(Span<char> destination, out int charsWritten)
    {
        charsWritten = 0;
        if (this.Count == 0)
        {
            return true;
        }

        destination[charsWritten++] = '?';

        ReadOnlySpan<IQueryField> span = CollectionsMarshal.AsSpan(_fields);
        ref readonly IQueryField field = ref span[0];
        if (!field.TryCopyToSlice(destination, ref charsWritten))
        {
            return false;
        }

        for (int i = 1; i < span.Length; i++)
        {
            destination[charsWritten++] = '&';
            field = ref span[i];
            if (!field.TryCopyToSlice(destination, ref charsWritten))
            {
                return false;
            }
        }

        return true;
    }

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
    {
        Span<char> chars = stackalloc char[_maxLength + 1 + (Math.Max(0, this.Count - 1))];
        _ = this.TryFormat(chars, out int charsWritten);

        return new string(chars.Slice(0, charsWritten));
    }
    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return this.TryFormat(destination, out charsWritten);
    }

    [DebuggerStepThrough]
    public IEnumerator<IQueryField> GetEnumerator()
    {
        return _fields.GetEnumerator();
    }
    [DebuggerStepThrough]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}