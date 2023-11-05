using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
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
        static readonly string _typeName = typeof(RenameObject).GetTypeName();

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
        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }

        int? IPipeable<ISeriesPipeable>.GetId()
        {
            return this.SeriesId;
        }
        int? IPipeable<IRenameFilePipeable>.GetId()
        {
            return this.EpisodeFileId;
        }
    }
}
