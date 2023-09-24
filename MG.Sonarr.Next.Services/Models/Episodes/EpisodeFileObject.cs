using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Shell.Models;

namespace MG.Sonarr.Next.Services.Models.Episodes
{
    public sealed class EpisodeFileObject : SonarrObject, IHasId, ISeriesPipeable
    {
        public int Id { get; private set; }
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
