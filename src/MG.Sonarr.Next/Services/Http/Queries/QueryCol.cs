using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Strings;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Services.Http.Queries;

[DebuggerDisplay(@"\{Count = {Count}, MaxLength = {MaxLength}\}")]
public sealed class QueryCol : IReadOnlyList<IQueryField>, ISpanFormattable
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int _maxLength;

    private readonly SortedList<string, IQueryField> _fields;

    public IQueryField this[int index]
    {
        get => _fields.Values[index];
        set => _fields.Values[index] = value;
    }

    public int Count => _fields.Count;
    public int MaxLength => _maxLength;

    [DebuggerStepThrough]
    public QueryCol()
    {
        _fields = [];
    }
    public QueryCol(int capacity)
    {
        _fields = new(capacity, StringComparer.OrdinalIgnoreCase);
    }

    public void AddBoolean(bool value, [CallerArgumentExpression(nameof(value))] string key = "")
    {
        this.Add([key, value]);
    }
    public void Add(params ReadOnlySpan<BooleanQueryField> fields)
    {
        foreach (BooleanQueryField field in fields)
        {
            _fields.Add(field.Key, field);
            _maxLength += field.MaxLength;
        }
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
        _fields.Add(field.Key, field);
        _maxLength += field.MaxLength;
    }
    public void AddOrUpdate(IQueryField field)
    {
        int index = _fields.IndexOfKey(field.Key);
        switch (index)
        {
            case -1:
                this.Add(field);
                break;

            case > -1:
                IQueryField existing = _fields.GetValueAtIndex(index);
                _maxLength += field.MaxLength - existing.MaxLength;
                _fields[field.Key] = field;
                break;

            default:
                throw new InvalidOperationException("Invalid index.");
        }
    }
    [DebuggerStepThrough]
    public void Clear()
    {
        _fields.Clear();
        _maxLength = 0;
    }
    public bool Remove(string key)
    {
        if (_fields.Remove(key, out IQueryField? field))
        {
            _maxLength -= field.MaxLength;
            return true;
        }

        return false;
    }
    public void RemoveAt(int index)
    {
        IQueryField field = _fields.GetValueAtIndex(index);
        _maxLength -= field.MaxLength;
        _fields.RemoveAt(index);
    }

    public bool TryFormat(Span<char> destination, out int charsWritten)
    {
        charsWritten = 0;
        if (this.Count == 0)
        {
            return true;
        }

        destination[charsWritten++] = '?';

        IQueryField field = _fields.GetValueAtIndex(0);
        if (!field.TryCopyToSlice(destination, ref charsWritten))
        {
            return false;
        }

        for (int i = 1; i < _fields.Count; i++)
        {
            destination[charsWritten++] = '&';
            field = _fields.GetValueAtIndex(i);
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

public static class QueryColExtensions
{
    public static string GetUrl(this QueryCol? collection, string baseUrl)
    {
        if (collection is null || collection.Count == 0)
        {
            return baseUrl;
        }

        Span<char> span = stackalloc char[baseUrl.Length + 1 + collection.MaxLength];
        int position = 0;

        baseUrl.CopyToSlice(span, ref position);
        collection.CopyToSlice(span, ref position, provider: Statics.DefaultProvider);

        return new string(span.Slice(0, position));
    }
}