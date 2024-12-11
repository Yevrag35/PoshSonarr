using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Strings;
using System;
using System.ComponentModel;

namespace MG.Sonarr.Next.Services.Http.Queries;

public sealed class PagingParameter : IQueryField
{
    const string PAGING_KEY = "Paging";
    private const string PAGE = "page";
    private const string PAGE_SIZE = "pageSize";
    private const string SORT_DIRECTION = "sortDirection";
    private const string SORT_KEY = "sortKey";

    const int DEFAULT_PAGE_NO = 1;
    const int DEFAULT_PAGE_SIZE = 10;
    static readonly int STARTING_LENGTH = GetStartingLength();

    private string _sortKey = string.Empty;

    public string Key => PAGING_KEY;
    public int MaxLength => STARTING_LENGTH + _sortKey.Length;
    public int Page { get; set; } = DEFAULT_PAGE_NO;
    public int PageSize { get; set; } = DEFAULT_PAGE_SIZE;
    [NotNull]
    public string? SortKey
    {
        get => _sortKey ??= string.Empty;
        set => _sortKey = value ?? string.Empty;
    }
    public ListSortDirection SortDirection { get; set; } = ListSortDirection.Descending;

    [DebuggerStepThrough]
    string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
    {
        Span<char> chars = stackalloc char[this.MaxLength];
        _ = this.TryFormat(chars, out int charsWritten);
        return new(chars.Slice(0, charsWritten));
    }
    public bool TryFormat(Span<char> destination, out int charsWritten)
    {
        charsWritten = 0;
        return TryWriteSection(destination, ref charsWritten, PAGE, this.Page)
            && TryWriteSection(destination, ref charsWritten, PAGE_SIZE, this.PageSize)
            && TryWriteSection(destination, ref charsWritten, SORT_DIRECTION, this.SortDirection)
            && TryWriteSection(destination, ref charsWritten, SORT_KEY, this.SortKey);
    }
    private static bool TryWriteSection(Span<char> destination, ref int charsWritten, ReadOnlySpan<char> name, int value)
    {
        if (!name.TryCopyToSlice(destination, ref charsWritten))
        {
            return false;
        }

        try
        {
            destination[charsWritten++] = '=';
            return value.TryCopyToSlice(destination, ref charsWritten, provider: Statics.DefaultProvider);
        }
        catch (IndexOutOfRangeException)
        {
            return false;
        }
    }
    private static bool TryWriteSection(Span<char> destination, ref int charsWritten, ReadOnlySpan<char> name, ListSortDirection direction)
    {
        if (!name.TryCopyToSlice(destination, ref charsWritten))
        {
            return false;
        }

        try
        {
            destination[charsWritten++] = '=';
            return direction switch
            {
                ListSortDirection.Ascending => nameof(ListSortDirection.Ascending).AsSpan().TryCopyToSlice(destination, ref charsWritten),
                ListSortDirection.Descending => nameof(ListSortDirection.Descending).AsSpan().TryCopyToSlice(destination, ref charsWritten),
                _ => ((int)direction).TryCopyToSlice(destination, ref charsWritten, provider: Statics.DefaultProvider),
            };
        }
        catch (IndexOutOfRangeException)
        {
            return false;
        }
    }
    private static bool TryWriteSection(Span<char> destination, ref int charsWritten, ReadOnlySpan<char> name, ReadOnlySpan<char> value)
    {
        if (!name.TryCopyToSlice(destination, ref charsWritten))
        {
            return false;
        }

        try
        {
            destination[charsWritten++] = '=';
            return value.TryCopyToSlice(destination, ref charsWritten);
        }
        catch (IndexOutOfRangeException)
        {
            return false;
        }
    }
    [DebuggerStepThrough]
    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return this.TryFormat(destination, out charsWritten);
    }

    [DebuggerStepThrough]
    static int CombineLengths(params ReadOnlySpan<string> values)
    {
        int length = 0;
        foreach (string value in values)
        {
            length += value.Length;
        }

        return length + values.Length + Math.Max(0, values.Length - 1);
    }
    [DebuggerStepThrough]
    static int GetStartingLength()
    {
        return (LengthConstants.INT_MAX * 2) + 10 + CombineLengths(PAGE, PAGE_SIZE, SORT_DIRECTION, SORT_KEY);
    }
}