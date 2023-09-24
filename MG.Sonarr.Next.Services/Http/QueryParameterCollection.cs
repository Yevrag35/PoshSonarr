using System.Collections;

namespace MG.Sonarr.Next.Services.Http
{
    public sealed class QueryParameterCollection : IReadOnlyCollection<QueryParameter>, ISpanFormattable
    {
        readonly HashSet<QueryParameter> _params;
        int _maxLength;

        public QueryParameter this[string key] => _params.TryGetValue(key, out QueryParameter actual)
            ? actual
            : default;

        public int MaxLength => _maxLength + (1 * (this.Count - 1));
        public int Count => _params.Count;

        public QueryParameterCollection(int capacity = 1)
        {
            _params = new(capacity);
        }

        public bool Add(string key, string? value)
        {
            var p = QueryParameter.Create(key, value);
            if (_params.Add(p))
            {
                _maxLength += p.MaxLength;
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool Add(string key, bool value)
        {
            return this.Add(key, value ? bool.TrueString : bool.FalseString);
        }
        public bool Add(string key, int value)
        {
            return this.Add(key, value, LengthConstants.INT_MAX);
        }
        public bool Add<T>(string key, T value, in int maxLength) where T : notnull, ISpanFormattable
        {
            QueryParameter p = QueryParameter.Create(key, value, in maxLength);
            if (_params.Add(p))
            {
                _maxLength += p.MaxLength;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Clear()
        {
            _params.Clear();
            _maxLength = 0;
        }

        public bool Remove(string key)
        {
            if (_params.TryGetValue(key, out var actual))
            {
                _maxLength -= actual.MaxLength;
                return _params.Remove(actual);
            }

            return false;
        }

        public string ToString(string? format, IFormatProvider? provider)
        {
            Span<char> chars = stackalloc char[this.MaxLength];
            _ = this.TryFormat(chars, out int written, format, provider);
            return new string(chars.Slice(0, written));
        }
        public bool TryFormat(Span<char> span, out int written, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            written = 0;
            int count = 0;
            foreach (QueryParameter p in _params)
            {
                try
                {
                    if (!p.TryFormat(span.Slice(written), out int wri, format, provider))
                    {
                        written += wri;
                        return false;
                    }
                    else
                    {
                        written += wri;
                    }
                }
                catch
                {
                    return false;
                }

                if (count < _params.Count - 1)
                {
                    span[written++] = '&';
                }

                count++;
            }

            return true;
        }

        public IEnumerator<QueryParameter> GetEnumerator()
        {
            return _params.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
