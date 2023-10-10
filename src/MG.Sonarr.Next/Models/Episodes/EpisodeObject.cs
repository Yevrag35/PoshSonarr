using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Episodes
{
    public sealed class EpisodeObject : SonarrObject,
        IComparable<EpisodeObject>,
        IEpisodeFilePipeable,
        IHasId,
        IJsonOnSerializing,
        IReleasePipeableByEpisode,
        ISeriesPipeable,
        ISerializableNames<EpisodeObject>
    {
        const int CAPACITY = 25;
        private DateOnly _airDate;

        public int AbsoluteEpisodeNumber { get; private set; }
        int IReleasePipeableByEpisode.EpisodeId => this.Id;
        public int EpisodeFileId { get; private set; }
        public int EpisodeNumber { get; private set; }
        public bool HasAbsolute { get; private set; }
        public int Id { get; private set; }
        public int SeasonNumber { get; private set; }
        public int SeriesId { get; private set; }

        public EpisodeObject()
            : base(CAPACITY)
        {
        }

        public int CompareTo(EpisodeObject? other)
        {
            if (other is null)
            {
                return -1;
            }

            return this.Id.CompareTo(other.Id);
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.EPISODE];
        }

        public override void OnDeserialized()
        {
            if (this.TryGetProperty("AirDate", out DateOnly airDate))
            {
                _airDate = airDate;
                this.Properties.Remove("AirDate");
            }

            if (this.TryGetId(out int id))
            {
                this.Id = id;
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
        }

        public void OnSerializing()
        {
            this.AddProperty("AirDate", _airDate);
        }
    }
}
