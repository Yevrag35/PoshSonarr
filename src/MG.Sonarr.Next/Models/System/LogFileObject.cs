using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.System
{
    public enum LogFileType
    {
        General,
        Update,
    }

    [SonarrObject]
    public sealed class LogFileObject : IdSonarrObject<LogFileObject>,
        IEquatable<LogFileObject>,
        ISerializableNames<LogFileObject>
    {
        const int CAPACITY = 5;
        const string UPDATE_PART = "/file/update/";

        public string ContentsUrl { get; private set; } = string.Empty;
        public DateTimeOffset LastWriteTime { get; private set; }
        public LogFileType Type
        {
            get => this.GetValue<LogFileType>();
            set => this.SetValue(value);
        }

        public LogFileObject()
            : base(CAPACITY)
        {
        }

        public override int CompareTo(LogFileObject? other)
        {
            int compare = Comparer<DateTimeOffset?>.Default.Compare(this.LastWriteTime, other?.LastWriteTime);
            if (compare == 0)
            {
                compare = StringComparer.InvariantCultureIgnoreCase.Compare(this.ContentsUrl, other?.ContentsUrl);
            }

            return compare;
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
            return HashCode.Combine(
                this.Id,
                this.Type,
                this.LastWriteTime,
                StringComparer.InvariantCultureIgnoreCase.GetHashCode(this.ContentsUrl));
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.LOG_FILE];
        }

        protected override void OnDeserialized(bool alreadyCalled)
        {
            base.OnDeserialized(alreadyCalled);
            this.Properties.Remove(nameof(this.Id));

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
        }
    }
}
