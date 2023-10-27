using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Calendar
{
    [SonarrObject]
    public sealed class CalendarObject : IdSonarrObject<CalendarObject>,
        IEpisodeFilePipeable,
        IEpisodePipeable,
        IRenameFilePipeable,
        ISeriesPipeable,
        ISerializableNames<CalendarObject>,
        ITagResolvable<CalendarObject>
    {
        const int CAPACITY = 20;
        static readonly string _typeName = typeof(CalendarObject).GetTypeName();
        public DateTimeOffset AirDateUtc { get; private set; }
        public int EpisodeFileId { get; private set; }
        int IEpisodePipeable.EpisodeId => this.Id;
        public int SeriesId { get; private set; }

        public CalendarObject()
            : base(CAPACITY)
        {
        }

        public override int CompareTo(CalendarObject? other)
        {
            if (other is null)
            {
                return -1;
            }

            int compare = this.AirDateUtc.CompareTo(other.AirDateUtc);
            if (compare == 0)
            {
                compare = this.Id.CompareTo(other.Id);
            }

            return compare;
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return GetTag(resolver);
        }

        public static MetadataTag GetTag(IMetadataResolver resolver)
        {
            return resolver[Meta.CALENDAR];
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();
            this.Properties.Remove("AirDate");

            if (this.TryGetProperty(nameof(this.SeriesId), out int seriesId))
            {
                this.SeriesId = seriesId;
            }

            if (this.TryGetProperty(nameof(this.AirDateUtc), out DateTimeOffset airDateUtc))
            {
                this.AirDateUtc = airDateUtc;
            }

            if (this.TryGetProperty(nameof(this.EpisodeFileId), out int epFileId))
            {
                this.EpisodeFileId = epFileId;
            }
        }
        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
    }
}
