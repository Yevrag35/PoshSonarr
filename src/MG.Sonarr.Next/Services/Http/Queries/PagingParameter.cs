using MG.Sonarr.Next.Extensions;
using System.ComponentModel;

namespace MG.Sonarr.Next.Services.Http.Queries
{
    public sealed class PagingParameter : IQueryParameter
    {
        const string PAGING_KEY = "Paging";
        const int DEFAULT_PAGE_NO = 1;
        const int DEFAULT_PAGE_SIZE = 10;
        //const string PAGE = "page";
        //const string PAGE_SIZE = "pageSize";
        //const string SORT_DIRECTION = "sortDirection";
        //const string SORT_KEY = "sortKey";

        private string _constructed = string.Empty;
        private bool _isConstructed;
        private int _maxLength;
        private int _pageNo;
        private int _pageSize;
        private string _sortKey;
        private ListSortDirection _direction;
        private int _hash;

        public string Key => PAGING_KEY;
        public int MaxLength => _maxLength;
        public int Page
        {
            get => _pageNo;
            set => _pageNo = CheckValue(in value, DEFAULT_PAGE_NO);
        }
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = CheckValue(in value, DEFAULT_PAGE_SIZE);
        }
        public ListSortDirection SortDirection
        {
            get => _direction;
            set
            {
                if (_direction == value)
                {
                    return;
                }

                _maxLength -= _direction.GetLength();
                _maxLength += value.GetLength();
                _direction = value;
            }
        }
        public string SortKey
        {
            get => _sortKey;
            set => _sortKey = SetStringField(value, _sortKey, Constants.ID, ref _maxLength);
        }

        public PagingParameter()
        {
            _direction = ListSortDirection.Descending;
            _pageNo = DEFAULT_PAGE_NO;
            _pageSize = DEFAULT_PAGE_SIZE;
            _sortKey = Constants.ID;
            _maxLength = GetStartingLength(_direction);
        }

        private static int CheckValue(in int value, int defaultValue)
        {
            return value > 0 ? value : defaultValue;
        }
        private static int GetStartingLength(ListSortDirection direction)
        {
            return (LengthConstants.INT_MAX * 2)
                   +
                   nameof(Page).Length + nameof(PageSize).Length
                   +
                   nameof(SortKey).Length + Constants.ID.Length
                   +
                   nameof(SortDirection).Length + direction.GetLength()
                   +
                   4;   // Equal signs for Page, PageSize, SortKey, SortDirection.
        }

        public bool Equals(IQueryParameter? other)
        {
            return StringComparer.InvariantCulture.Equals(PAGING_KEY, other?.Key);
        }
        public bool Equals(QueryParameter other)
        {
            return StringComparer.InvariantCulture.Equals(PAGING_KEY, other.Key);
        }
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            else if (obj is PagingParameter pp)
            {
                return this.Equals(pp);
            }
            else if (obj is IQueryParameter qp)
            {
                return this.Equals(qp);
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            return PAGING_KEY.GetHashCode();
        }
        private int GetChangedHashCode(string? format, IFormatProvider? provider)
        {
            format ??= string.Empty;
            provider ??= Statics.DefaultProvider;

            return HashCode.Combine(_direction, _pageNo, _pageSize, _sortKey, format, provider);
        }
        public bool IsHashChanged(int current, string? format, IFormatProvider? provider, out int changedHashCode)
        {
            changedHashCode = this.GetChangedHashCode(format, provider);

            return current != changedHashCode;
        }

        public void Reset()
        {
            _pageNo = DEFAULT_PAGE_NO;
            _pageSize = DEFAULT_PAGE_SIZE;
            _sortKey = Constants.ID;
            _direction = ListSortDirection.Descending;
            _maxLength = GetStartingLength(_direction);
        }

        private static string SetStringField(string? incoming, string current, [ConstantExpected] string defaultValue, ref int maxLength)
        {
            if (string.IsNullOrWhiteSpace(incoming))
            {
                maxLength -= current.Length;
                maxLength += defaultValue.Length;
                return defaultValue;
            }
            else if (current.Equals(incoming, StringComparison.InvariantCultureIgnoreCase))
            {
                return current;
            }

            maxLength -= current.Length;
            maxLength += incoming.Length;
            return incoming;
        }
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            if (_isConstructed && !this.IsHashChanged(_hash, format, formatProvider, out int changed))
            {
                return _constructed;
            }
            else
            {
                Debug.WriteLine("PagingParameter hash is changed or not set.");
                changed = this.GetChangedHashCode(format, formatProvider);
            }

            Span<char> span = stackalloc char[_maxLength];
            _ = this.TryFormatInternal(span, out int written);
            _constructed = new string(span.Slice(0, written));
            _isConstructed = true;
            _hash = changed;

            return _constructed;
        }
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            charsWritten = 0;
            if (_isConstructed && !this.IsHashChanged(_hash, string.Empty, provider, out _))
            {
                _constructed.CopyToSlice(destination, ref charsWritten);
                return true;
            }
            else
            {
                return this.TryFormatInternal(destination, out charsWritten);
            }
        }

        private bool TryFormatInternal(Span<char> destination, out int charsWritten)
        {
            charsWritten = 0;
            WriteIntSection(destination, nameof(this.Page), _pageNo, ref charsWritten);
            WriteIntSection(destination, nameof(this.PageSize), _pageSize, ref charsWritten);
            WriteStringSection(destination, nameof(this.SortDirection), _direction.ToString(), includeAnd: true, ref charsWritten);
            WriteStringSection(destination, nameof(this.SortKey), _sortKey, ref charsWritten);

            return true;
        }

        private static void WriteKey(Span<char> span, ReadOnlySpan<char> key, ref int position)
        {
            if (char.IsLower(key[0]))
            {
                key.CopyToSlice(span, ref position);
                return;
            }

            Span<char> working = span.Slice(position);
            working[0] = char.ToLower(key[0]);
            key.Slice(1).CopyTo(working.Slice(1));

            position += key.Length;
        }
        private static void WriteIntSection(Span<char> span, ReadOnlySpan<char> key, int value, ref int position)
        {
            WriteKey(span, key, ref position);
            span[position++] = '=';
            if (!value.TryFormat(span.Slice(position), out int written, default, Statics.DefaultProvider))
            {
                Debug.Fail($"Couldn't format int value -> {value}");

                ReadOnlySpan<char> toStr = value.ToString().AsSpan();
                toStr.CopyTo(span.Slice(position));
                position += toStr.Length;
            }
            else
            {
                position += written;
            }

            span[position++] = '&';
        }
        private static void WriteStringSection(Span<char> span, ReadOnlySpan<char> key, ReadOnlySpan<char> value, bool includeAnd, ref int position)
        {
            WriteStringSection(span, key, value, ref position);
            if (includeAnd)
            {
                span[position++] = '&';
            }
        }
        private static void WriteStringSection(Span<char> span, ReadOnlySpan<char> key, ReadOnlySpan<char> value, ref int position)
        {
            WriteKey(span, key, ref position);
            span[position++] = '=';

            int written = value.ToLower(span.Slice(position), Statics.DefaultCulture);
            position += written;
        }
    }
}
