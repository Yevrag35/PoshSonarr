using MG.Sonarr.Next.Extensions;

namespace MG.Sonarr.Next.Models.Episodes
{
    public readonly struct EpisodeRange
    {
        readonly int _start;
        readonly int _end;
        readonly bool _isNotEmpty;
        readonly bool _isSingle;
        readonly bool _allDontMatch;

        public bool AllMatch => !_allDontMatch;
        public bool IsSingle => _isSingle;
        public int Start => _start;
        public int End => _end;

        private EpisodeRange(bool isEmpty)
        {
            _isNotEmpty = true;
            _isSingle = true;
            _start = -1;
            _end = -1;
            _allDontMatch = false;
        }
        public EpisodeRange(in int start, in int end)
        {
            _start = start;
            _end = end;
            _isNotEmpty = true;
            _isSingle = start == end;
            _allDontMatch = start > -1 && end > -1;
        }

        public bool IsInRange(in int episodeNumber)
        {
            if (this.AllMatch)
            {
                return true;
            }

            return _isSingle
                ? episodeNumber == _start
                : episodeNumber >= _start && episodeNumber <= _end;
        }
        public bool IsValid()
        {
            return _isNotEmpty && _start > -1 && _end > -1;
        }

        public static readonly EpisodeRange Empty = new(true);

        const string ALL_MATCH = "All Match";
        //static readonly int PART_LENGTH = nameof(Start).Length + nameof(End).Length + 8;

        public override string ToString()
        {
            if (!_isNotEmpty)
            {
                return this.GetType().GetTypeName();
            }
            else if (this.AllMatch)
            {
                return ALL_MATCH;
            }

            ReadOnlySpan<char> startName = nameof(this.Start);
            ReadOnlySpan<char> endName = nameof(this.End);

            int length = startName.Length + endName.Length
                + 8
                + (LengthConstants.INT_MAX * 2);

            Span<char> span = stackalloc char[length];
            Span<char> separator = stackalloc char[] { ' ', '=', ' ' };

            int position = 0;
            startName.CopyToSlice(span, ref position);
            separator.CopyToSlice(span, ref position);

            _ = _start.TryFormat(span.Slice(position), out int written);
            position += written;

            span[position++] = ';';
            endName.CopyToSlice(span, ref position);
            separator.CopyToSlice(span, ref position);

            _ = _end.TryFormat(span.Slice(position), out written);
            position += written;

            return new string(span.Slice(0, position));
        }
    }
}

