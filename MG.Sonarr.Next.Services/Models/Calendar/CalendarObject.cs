using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Services.Models.Calendar
{
    public sealed class CalendarObject : SonarrObject, ISeriesPipeable, IEpisodePipeable
    {
        public DateTimeOffset AirDateUtc { get; private set; }
        public int Id { get; private set; }
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

            if (this.TryGetProperty(nameof(this.AirDateUtc), out DateTimeOffset airDateUtc))
            {
                this.AirDateUtc = airDateUtc;
            }
        }
    }
}
