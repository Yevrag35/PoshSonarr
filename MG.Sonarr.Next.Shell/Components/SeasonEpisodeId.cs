using MG.Sonarr.Next.Services;

namespace MG.Sonarr.Next.Shell.Components
{
    public readonly struct SeasonEpisodeId
    {
        readonly int _season;
        readonly int _episode;
        readonly bool _isAbsolute;
        readonly bool _isNotEmpty;

        public int Episode => _episode;
        public bool IsAbsolute => _isAbsolute;
        public bool IsEmpty => !_isNotEmpty;
        public int Season => _season;

        private SeasonEpisodeId(in int absoluteNumber)
        {
            _season = -1;
            _episode = absoluteNumber;
            _isAbsolute = true;
            _isNotEmpty = true;
        }
        private SeasonEpisodeId(in int season, in int episode, bool isAbsolute)
        {
            _season = season;
            _episode = episode;
            _isAbsolute = isAbsolute;
            _isNotEmpty = season > 0 || episode > 0;
        }

        public static bool TryParse(ReadOnlySpan<char> value, out SeasonEpisodeId result)
        {
            result = default;
            if (value.IsWhiteSpace())
            {
                return false;
            }

            char e = 'e';
            int episode = 0;
            int index = value.IndexOf(new ReadOnlySpan<char>(in e), StringComparison.InvariantCultureIgnoreCase);

            if (index > -1)
            {
                var epSlice = value.Slice(index + 1);
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
                result = new SeasonEpisodeId(in season, in episode, true);
                return true;
            }

            index = value.IndexOf(new ReadOnlySpan<char>(in s), StringComparison.InvariantCultureIgnoreCase);
            if (index > -1)
            {
                var sSlice = value.Slice(index + 1);
                if (int.TryParse(sSlice, Statics.DefaultProvider, out season))
                {
                    result = new(in season, in episode, false);
                    return true;
                }
            }
            else if (int.TryParse(value, Statics.DefaultProvider, out int absolute))
            {
                result = new(in season, in absolute, true);
                return true;
            }

            return false;
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
