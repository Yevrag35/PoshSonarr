using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Episodes
{
    public sealed class EpisodeFileObject : SonarrObject,
        IEpisodeFilePipeable,
        IHasId,
        ISeriesPipeable
    {
        const int CAPACITY = 14;

        public int Id { get; private set; }
        int IEpisodeFilePipeable.EpisodeFileId => this.Id;
        public int SeriesId { get; private set; }

        public EpisodeFileObject()
            : base(CAPACITY)
        {
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.EPISODE_FILE];
        }

        public override void OnDeserialized()
        {
            if (this.TryGetId(out int id))
            {
                this.Id = id;
            }

            if (this.TryGetProperty(nameof(this.SeriesId), out int seriesId))
            {
                this.SeriesId = seriesId;
            }
        }
    }
}
