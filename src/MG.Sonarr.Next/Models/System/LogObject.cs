using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;

namespace MG.Sonarr.Next.Services.Models.System
{
    public sealed class LogObject : SonarrObject, IComparable<LogObject>
    {
        public string Level { get; private set; } = string.Empty;
        public DateTimeOffset Time { get; private set; }

        public LogObject() : base(5)
        {
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.LOG_ITEM];
        }

        public int CompareTo(LogObject? other)
        {
            DateTimeOffset offset = other?.Time ?? default;
            return this.Time.CompareTo(offset);
        }

        public override void OnDeserialized()
        {
            if (this.TryGetNonNullProperty(nameof(this.Level), out string? level))
            {
                this.Level = level;
            }

            if (this.TryGetProperty(nameof(this.Time), out DateTimeOffset time))
            {
                this.Time = time;
            }
        }
    }
}
