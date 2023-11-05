using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
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
        static readonly string _typeName = typeof(EpisodeFileObject).GetTypeName();

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

        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }

        int? IPipeable<IEpisodeFilePipeable>.GetId()
        {
            return this.Id;
        }
        int? IPipeable<IRenameFilePipeable>.GetId()
        {
            return this.Id;
        }
        int? IPipeable<ISeriesPipeable>.GetId()
        {
            return this.SeriesId;
        }
    }
}
