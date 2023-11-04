using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Series;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Episodes
{
    [SonarrObject]
    public sealed class EpisodeObject : IdSonarrObject<EpisodeObject>,
        IEpisodeFilePipeable,
        IHasId,
        IJsonOnSerializing,
        IReleasePipeableByEpisode,
        IRenameFilePipeable,
        ISeriesPipeable,
        ISerializableNames<EpisodeObject>
    {
        const int CAPACITY = 25;
        static readonly string _typeName = typeof(EpisodeObject).GetTypeName();
        private DateOnly _airDate;

        public int AbsoluteEpisodeNumber { get; private set; }
        int IReleasePipeableByEpisode.EpisodeId => this.Id;
        public int EpisodeFileId { get; private set; }
        public int EpisodeNumber { get; private set; }
        public bool HasAbsolute { get; private set; }
        public int SeasonNumber { get; private set; }
        public int SeriesId { get; private set; }
        public IComparable Series { get; private set; } = 0;

        public EpisodeObject()
            : base(CAPACITY)
        {
        }

        public override int CompareTo(EpisodeObject? other)
        {
            int compare = this.Series.CompareTo(other?.Series);
            if (compare != 0)
            {
                return compare;
            }

            compare = this.SeasonNumber.CompareTo(other?.SeasonNumber);
            if (compare == 0)
            {
                compare = this.EpisodeNumber.CompareTo(other?.EpisodeNumber);
                if (compare == 0)
                {
                    compare = this.AbsoluteEpisodeNumber.CompareTo(other?.AbsoluteEpisodeNumber);
                }
            }

            return compare;
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.EPISODE];
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();
            if (this.TryGetProperty("AirDate", out DateOnly airDate))
            {
                _airDate = airDate;
                this.Properties.Remove("AirDate");
            }

            if (this.TryGetProperty(nameof(this.AbsoluteEpisodeNumber), out int abId))
            {
                this.HasAbsolute = true;
            }

            this.AbsoluteEpisodeNumber = abId;

            if (this.TryGetProperty(nameof(this.EpisodeFileId), out int epFileId))
            {
                this.EpisodeFileId = epFileId;
            }

            if (this.TryGetProperty(nameof(this.EpisodeNumber), out int epNumber))
            {
                this.EpisodeNumber = epNumber;
            }

            if (this.TryGetProperty(nameof(this.SeasonNumber), out int seasonNumber))
            {
                this.SeasonNumber = seasonNumber;
            }

            if (this.TryGetProperty(nameof(this.SeriesId), out int seriesId))
            {
                this.SeriesId = seriesId;
            }

            if (this.TryGetNonNullProperty(nameof(this.Series), out SeriesObject? so))
            {
                this.Series = so.Title;
            }
            else
            {
                this.Series = this.SeriesId;
            }
        }

        public void OnSerializing()
        {
            this.AddProperty("AirDate", _airDate);
        }

        public void SetSeries(SeriesObject series)
        {
            this.Series = series.Title;
        }

        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
    }
}
