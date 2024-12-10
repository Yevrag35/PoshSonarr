using System;
using System.ComponentModel;

namespace MG.Sonarr.Next.Services.Http.Queries;

public sealed class PagingParameter : IQueryField
{
    const string PAGING_KEY = "Paging";
    const int DEFAULT_PAGE_NO = 1;
    const int DEFAULT_PAGE_SIZE = 10;
    static readonly int STARTING_LENGTH = LengthConstants.INT_MAX * 2 + 
    private int _maxLength;

    public string Key => PAGING_KEY;
    public int MaxLength => throw new NotImplementedException();

    [DebuggerStepThrough]
    string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
    {
        
    }
    public bool TryFormat(Span<char> destination, out int charsWritten)
    {

    }
    [DebuggerStepThrough]
    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return this.TryFormat(destination, out charsWritten);
    }
}