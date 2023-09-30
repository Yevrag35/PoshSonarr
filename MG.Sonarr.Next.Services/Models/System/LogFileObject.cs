using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;

namespace MG.Sonarr.Next.Services.Models.System
{
    public sealed class LogFileObject : SonarrObject, IComparable<LogFileObject>
    {
        public string ContentsUrl { get; private set; } = string.Empty;
        public DateTimeOffset LastWriteTime { get; private set; }

        public LogFileObject() : base(5)
        {
        }

        public int CompareTo(LogFileObject? other)
        {
            return other is not null ? this.LastWriteTime.CompareTo(other.LastWriteTime) : 1;
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.LOG_FILE];
        }

        public override void OnDeserialized()
        {
            if (this.TryGetNonNullProperty(nameof(this.ContentsUrl), out string? cl))
            {
                this.ContentsUrl = cl;
            }

            if (this.TryGetProperty(nameof(this.LastWriteTime), out DateTimeOffset lastWriteTime))
            {
                this.LastWriteTime = lastWriteTime;
            }

            base.OnDeserialized();
        }
    }
}
