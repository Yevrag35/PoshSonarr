using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Models.Episodes;

namespace MG.Sonarr.Next.Shell.Components
{
    [DebuggerDisplay(@"{GetDebugString()}")]
    public readonly struct SeasonEpisodeId : IEpisodeIdentifier
    {
        readonly int _season;
        readonly bool _isAbsolute;
        readonly bool _isNotEmpty;
        readonly EpisodeRange _range;

        public EpisodeRange EpisodeRange => _range;
        public bool IsAbsolute => _isAbsolute;
        public bool IsEmpty => !_isNotEmpty;
        public int Season => _season;

        private SeasonEpisodeId(in int absoluteNumber)
        {
            _season = -1;
            _isAbsolute = true;
            _isNotEmpty = true;
            _range = new(in absoluteNumber, in absoluteNumber);
        }
        private SeasonEpisodeId(in int season, in int episode, bool isAbsolute)
        {
            _season = season;
            _isAbsolute = isAbsolute;
            _isNotEmpty = season > 0 || episode > 0;
            _range = new(in episode, in episode);
        }
        private SeasonEpisodeId(in int season, EpisodeRange epRange, bool isAbsolute)
        {
            _season = season;
            _isAbsolute = isAbsolute;
            _isNotEmpty = season > 0 || epRange.IsValid();
            _range = epRange;
        }

        bool IEpisodeIdentifier.IsValid()
        {
            return _isNotEmpty;
        }

        public static bool TryParse([ValidatedNotNull] ReadOnlySpan<char> value, out SeasonEpisodeId result)
        {
            result = default;
            if (value.IsWhiteSpace())
            {
                return false;
            }

            char e = 'e';
            int endEp = -1;
            int episode = -1;
            int index = value.IndexOf(new ReadOnlySpan<char>(in e), StringComparison.InvariantCultureIgnoreCase);

            if (index > -1)
            {
                var epSlice = value.Slice(index + 1);
                int dashIndex = epSlice.IndexOf('-');
                if (dashIndex > -1)
                {
                    var endSlice = epSlice.Slice(dashIndex + 1);
                    if (int.TryParse(endSlice, Statics.DefaultProvider, out int endEpNum))
                    {
                        endEp = endEpNum;
                        epSlice = epSlice.Slice(0, dashIndex);
                    }
                }

                if (int.TryParse(epSlice, Statics.DefaultProvider, out int epNum))
                {
                    episode = epNum;
                }

                value = value.Slice(0, index);
            }

            char s = 's';
            int season = 0;
            if (value.IsWhiteSpace())
            {
                result = endEp > -1
                    ? new(in season, new EpisodeRange(in episode, in endEp), true)
                    : new(in season, in episode, true);
                return true;
            }

            index = value.IndexOf(new ReadOnlySpan<char>(in s), StringComparison.InvariantCultureIgnoreCase);
            if (index > -1)
            {
                var sSlice = value.Slice(index + 1);

                if (int.TryParse(sSlice, Statics.DefaultProvider, out season))
                {
                    //result = new(in season, in episode, false);
                    result = endEp > -1
                        ? new(in season, new EpisodeRange(in episode, in endEp), false)
                        : new(in season, in episode, false);

                    return true;
                }
            }
            else if (int.TryParse(value, Statics.DefaultProvider, out int absolute))
            {
                //result = new(in season, in absolute, true);
                result = endEp > -1
                    ? new(in season, new EpisodeRange(in absolute, in endEp), true)
                    : new(in season, in absolute, true);

                return true;
            }

            return false;
        }

        public static SeasonEpisodeId[] FromArray(object[] array)
        {
            ArgumentNullException.ThrowIfNull(array);
            if (array.Length <= 0)
            {
                return Array.Empty<SeasonEpisodeId>();
            }

            SeasonEpisodeId[] copyTo = new SeasonEpisodeId[array.Length];

            for (int i = 0; i < array.Length; i++)
            {
                object o = array[i];
                switch (o)
                {
                    case SeasonEpisodeId sei:
                        copyTo[i] = sei;
                        break;

                    case int intVal:
                        copyTo[i] = intVal;
                        break;

                    case long longVal:
                        if (longVal <= int.MaxValue)
                        {
                            copyTo[i] = Convert.ToInt32(longVal);
                            break;
                        }
                        else
                        {
                            goto default;
                        }

                    case string strVal:
                        copyTo[i] = strVal;
                        break;

                    default:
                        copyTo[i] = default;
                        break;
                }
            }

            return copyTo;
        }

        private string GetDebugString()
        {
            Span<char> span = stackalloc char[3 + (LengthConstants.INT_MAX * 2)];
            span[0] = 'S';
            this.Season.TryFormat(span.Slice(1), out int written);
            int pos = 1 + written;

            span[pos++] = 'E';
            _ = this.EpisodeRange.Start.TryFormat(span.Slice(pos), out written);
            pos += written;

            if (!this.EpisodeRange.IsSingle)
            {
                span[pos++] = '-';
                _ = this.EpisodeRange.End.TryFormat(span.Slice(pos), out written);
                pos += written;
            }

            return new string(span.Slice(0, pos));
        }

        public static implicit operator SeasonEpisodeId(string? value)
        {
            return TryParse(value, out var id) ? id : default;
        }
        public static implicit operator SeasonEpisodeId(int absoluteEpNumber)
        {
            return new(in absoluteEpNumber);
        }
    }
}
