using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;

namespace MG.Sonarr.Next.Services.Models.Episodes
{
    public sealed class EpisodeObject : SonarrObject, IEpisodeFilePipeable, ISeriesPipeable
    {
        public int AbsoluteEpisodeNumber { get; private set; }
        public int EpisodeFileId { get; private set; }
        public int EpisodeNumber { get; private set; }
        public bool HasAbsolute { get; private set; }
        public int Id { get; private set; }
        public int SeasonNumber { get; private set; }
        public int SeriesId { get; private set; }

        public EpisodeObject()
            : base(25)
        {
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.EPISODE];
        }

        public override void OnDeserialized()
        {
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
    }
}
