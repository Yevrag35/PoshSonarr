using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.History
{
    [SonarrObject]
    public sealed class HistoryObject : IdSonarrObject<HistoryObject>,
        ISerializableNames<HistoryObject>
    {
        const int CAPACITY = 14;
        static readonly string _typeName = typeof(HistoryObject).GetTypeName();

        public string DownloadId { get; private set; } = string.Empty;
        public int EpisodeId { get; private set; }
        public int SeriesId { get; private set; }

        public HistoryObject()
            : base(CAPACITY)
        {
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.HISTORY];
        }
        protected override void OnDeserialized(bool alreadyCalled)
        {
            base.OnDeserialized(alreadyCalled);
            if (this.TryGetNonNullProperty(nameof(this.DownloadId), out string? dlId))
            {
                this.DownloadId = dlId;
            }

            if (this.TryGetProperty(nameof(this.EpisodeId) ,out int epId))
            {
                this.EpisodeId = epId;
            }

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
    }
}
