using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Shell.Models;

namespace MG.Sonarr.Next.Models.Episodes
{
    public sealed class EpisodeFileObject : SonarrObject,
        IEpisodeFilePipeable,
        IHasId,
        ISeriesPipeable
    {
        public int Id { get; private set; }
        int IEpisodeFilePipeable.EpisodeFileId => this.Id;
        public int SeriesId { get; private set; }

        public EpisodeFileObject()
            : base(14)
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
