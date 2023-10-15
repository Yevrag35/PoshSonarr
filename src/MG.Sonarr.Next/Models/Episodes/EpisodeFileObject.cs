using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Episodes
{
    [SonarrObject]
    public sealed class EpisodeFileObject : IdSonarrObject<EpisodeFileObject>,
        IEpisodeFilePipeable,
        IHasId,
        IRenameFilePipeable,
        ISeriesPipeable,
        ISerializableNames<EpisodeFileObject>
    {
        const int CAPACITY = 14;

        int IEpisodeFilePipeable.EpisodeFileId => this.Id;
        int IRenameFilePipeable.EpisodeFileId => this.Id;
        public int SeriesId { get; private set; }

        public EpisodeFileObject()
            : base(CAPACITY)
        {
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.EPISODE_FILE];
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();

            if (this.TryGetProperty(nameof(this.SeriesId), out int seriesId))
            {
                this.SeriesId = seriesId;
            }
        }
    }
}
