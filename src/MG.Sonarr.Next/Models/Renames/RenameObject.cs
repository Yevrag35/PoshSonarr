using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Renames
{
    [SonarrObject]
    public sealed class RenameObject : SonarrObject,
        IComparable<RenameObject>,
        IJsonOnSerializing,
        ISeriesPipeable,
        IRenameFilePipeable,
        ISerializableNames<RenameObject>
    {
        const int CAPACITY = 6;

        public int EpisodeFileId { get; private set; }
        public int SeriesId { get; private set; }

        public RenameObject()
            : base(CAPACITY)
        {
        }

        public int CompareTo(RenameObject? other)
        {
            int compare = Comparer<int?>.Default.Compare(this.SeriesId, other?.SeriesId);
            if (compare == 0)
            {
                compare = Comparer<int?>.Default.Compare(this.EpisodeFileId, other?.EpisodeFileId);
            }

            return compare;
        }
        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.RENAMABLE];
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();
            if (this.TryGetProperty(nameof(this.SeriesId), out int seriesId))
            {
                this.SeriesId = seriesId;
            }

            if (this.TryGetProperty(nameof(this.EpisodeFileId), out int episodeFileId))
            {
                this.EpisodeFileId = episodeFileId;
            }
        }
        public void OnSerializing()
        {
            this.UpdateProperty(x => x.EpisodeFileId);
            this.UpdateProperty(x => x.SeriesId);
        }
    }
}
