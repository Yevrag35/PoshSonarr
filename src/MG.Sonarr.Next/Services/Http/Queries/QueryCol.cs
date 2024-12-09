using MG.Sonarr.Next.Unions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Services.Http.Queries;

public sealed class QueryCol
{
    static readonly string s_True = bool.TrueString.ToLower();
    static readonly string s_False = bool.FalseString.ToLower();

    private readonly List<IQueryField> _fields;
    private int _maxLength;

    public int Count => _fields.Count;
    public int MaxLength => _maxLength;

    public QueryCol()
    {
        _fields = [];
    }

    public void Add(string key, bool value)
    {
        this.Add(key, value ? s_True : s_False);
    }
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

        int length = 1 + _maxLength + Math.Max(_fields.Count - 1, 0);
        destination[charsWritten++] = '?';

        ReadOnlySpan<IQueryField> span = CollectionsMarshal.AsSpan(_fields);
        ref readonly IQueryField field = ref span[0];
        if (!field.TryFormat(destination.Slice(charsWritten), out int written, default, null))
        {
            return false;
        }

        charsWritten += written;

        for (int i = 1; i < span.Length; i++)
        {
            destination[charsWritten++] = '&';
            field = ref span[i];
            if (!field.TryFormat(destination.Slice(charsWritten), out written, default, null))
            {
                return false;
            }

            charsWritten += written;
        }

        return true;
    }
}