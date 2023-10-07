using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;

namespace MG.Sonarr.Next.Services.Models.System
{
    public enum LogFileType
    {
        General,
        Update,
    }

    public sealed class LogFileObject : SonarrObject, IComparable<LogFileObject>, IEquatable<LogFileObject>
    {
        const string UPDATE_PART = "/file/update/";

        public int Id { get; private set; }
        public string ContentsUrl { get; private set; } = string.Empty;
        public DateTimeOffset LastWriteTime { get; private set; }
        public LogFileType Type
        {
            get => this.GetValue<LogFileType>();
            set => this.SetValue(value);
        }

        public LogFileObject() : base(5)
        {
        }

        public int CompareTo(LogFileObject? other)
        {
            return other is not null ? this.LastWriteTime.CompareTo(other.LastWriteTime) : 1;
        }
        public bool Equals(LogFileObject? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            else if (other is null)
            {
                return false;
            }

            return this.Id == other.Id
                   &&
                   this.Type == other.Type
                   &&
                   StringComparer.InvariantCultureIgnoreCase.Equals(this.ContentsUrl, other.ContentsUrl);
        }
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            else if (obj is LogFileObject lfo)
            {
                return this.Equals(lfo);
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Id, this.Type,
                StringComparer.InvariantCultureIgnoreCase.GetHashCode(this.ContentsUrl));
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.LOG_FILE];
        }

        public override void OnDeserialized()
        {
            if (this.TryGetId(out int id))
            {
                this.Id = id;
                this.Properties.Remove(nameof(this.Id));
            }

            if (this.TryGetNonNullProperty(nameof(this.ContentsUrl), out string? cl))
            {
                this.ContentsUrl = cl;
                this.Type = cl.Contains(UPDATE_PART, StringComparison.InvariantCultureIgnoreCase)
                    ? LogFileType.Update
                    : LogFileType.General;
            }

            if (this.TryGetProperty(nameof(this.LastWriteTime), out DateTimeOffset lastWriteTime))
            {
                this.LastWriteTime = lastWriteTime;
            }

            base.OnDeserialized();
        }
    }
}
