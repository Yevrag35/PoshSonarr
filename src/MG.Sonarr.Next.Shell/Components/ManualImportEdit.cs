using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.ManualImports;
using MG.Sonarr.Next.Models.Qualities;
using MG.Sonarr.Next.Models.Series;

namespace MG.Sonarr.Next.Shell.Components
{
    internal sealed class ManualImportEdit
    {
        EpisodeObject? _episode;
        QualityRevisionObject? _quality;
        SeriesObject? _series;
        bool _hasEpisode;
        bool _hasQuality;
        bool _hasSeries;

        [MemberNotNullWhen(true, nameof(Episode), nameof(_episode))]
        internal bool HasEpisode => _hasEpisode;

        [MemberNotNullWhen(true, nameof(Quality), nameof(_quality))]
        internal bool HasQuality => _hasQuality;

        [MemberNotNullWhen(true, nameof(Series), nameof(_series))]
        internal bool HasSeries => _hasSeries;

        internal EpisodeObject? Episode
        {
            get => _episode;
            set
            {
                _episode = value;
                _hasEpisode = _episode is not null;
            }
        }
        internal QualityRevisionObject? Quality
        {
            get => _quality;
            set
            {
                _quality = value;
                _hasQuality = _quality is not null;
            }
        }
        internal SeriesObject? Series
        {
            get => _series;
            set
            {
                _series = value;
                _hasSeries = _series is not null;
            }
        }

        public ManualImportEdit()
        {
        }

        internal void EditImport(ManualImportObject importObj)
        {
            ArgumentNullException.ThrowIfNull(importObj);

            if (this.HasEpisode)
            {
                importObj.Episodes.Add(this.Episode);
            }

            if (this.HasQuality)
            {
                importObj.Quality = this.Quality;
            }

            if (this.HasSeries)
            {
                importObj.Series = this.Series;
            }
        }
    }
}

