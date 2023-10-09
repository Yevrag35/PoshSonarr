using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Calendar
{
    public sealed class CalendarObject : SonarrObject,
        IComparable<CalendarObject>,
        IEpisodeFilePipeable,
        IEpisodePipeable,
        ISeriesPipeable,
        ISerializableNames<CalendarObject>,
        ITagResolvable<CalendarObject>
    {
        const int CAPACITY = 20;

        public DateTimeOffset AirDateUtc { get; private set; }
        public int Id { get; private set; }
        public int EpisodeFileId { get; private set; }
        int IEpisodePipeable.EpisodeId => this.Id;
        public int SeriesId { get; private set; }

        public CalendarObject()
            : base(CAPACITY)
        {
        }

        public int CompareTo(CalendarObject? other)
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

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.CALENDAR];
        }

        public static MetadataTag GetTag(MetadataResolver resolver)
        {
            return resolver[Meta.CALENDAR];
        }

        public override void OnDeserialized()
        {
            this.Properties.Remove("AirDate");
            if (this.TryGetId(out int id))
            {
                this.Id = id;
            }

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
    }
}
