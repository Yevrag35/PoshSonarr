using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;

namespace MG.Sonarr.Next.Services.Models.Calendar
{
    public sealed class CalendarObject : SonarrObject, ISeriesPipeable, IEpisodePipeable, IEpisodeFilePipeable, ITagResolvable<CalendarObject>
    {
        public DateTimeOffset AirDateUtc { get; private set; }
        public int Id { get; private set; }
        public int EpisodeFileId { get; private set; }
        int IEpisodePipeable.EpisodeId => this.Id;
        public int SeriesId { get; private set; }

        public CalendarObject()
            : base(20)
        {
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
