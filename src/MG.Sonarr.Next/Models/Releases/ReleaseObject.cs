using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Releases
{
    [SonarrObject]
    public sealed class ReleaseObject : SonarrObject,
        IComparable<ReleaseObject>,
        IJsonOnSerializing,
        ISerializableNames<ReleaseObject>
    {
        const int CAPACITY = 46;

        int _age;
        Weight _weight;
        double _ageHours;
        double _ageMinutes;

        public TimeSpan Age { get; private set; }
        public int IndexerId { get; private set; }
        public string ReleaseUrl { get; private set; } = string.Empty;
        public int TotalWeight => _weight.TotalWeight;

        public ReleaseObject()
            : base(CAPACITY)
        {
        }

        public int CompareTo(ReleaseObject? other)
        {
            int compare = Comparer<int?>.Default.Compare(this.TotalWeight, other?.TotalWeight);
            if (compare != 0)
            {
                return compare;
            }

            compare = Comparer<TimeSpan?>.Default.Compare(this.Age, other?.Age);
            if (compare == 0)
            {
                compare = Comparer<int?>.Default.Compare(this.IndexerId, other?.IndexerId);
                if (compare == 0)
                {
                    compare = StringComparer.InvariantCultureIgnoreCase.Compare(this.ReleaseUrl, other?.ReleaseUrl);
                }
            }

            return compare;
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.RELEASE];
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();
            if (this.TryGetProperty(nameof(this.IndexerId), out int indexerId))
            {
                this.IndexerId = indexerId;
            }

            if (this.TryGetProperty("AgeMinutes", out double value))
            {
                _ageMinutes = value;
            }

            if (this.TryGetProperty("AgeHours", out double hours))
            {
                _ageHours = hours;
            }

            if (this.TryGetProperty(nameof(this.Age), out int days))
            {
                _age = days;
            }

            if (this.TryGetNonNullProperty("Guid", out string? releaseUrl))
            {
                this.ReleaseUrl = releaseUrl;
            }

            _weight = new(this);
            this.AddProperty(nameof(this.TotalWeight), this.TotalWeight);

            this.Age = TimeSpan.FromMinutes(_ageMinutes);
            this.Reset();
        }

        public void OnSerializing()
        {
            _weight.SetRelease(this);
            this.UpdateProperty(nameof(this.Age), _age);
            this.UpdateProperty(nameof(this.IndexerId), this.IndexerId);
            this.UpdateProperty(nameof(this.ReleaseUrl), this.ReleaseUrl);
            this.AddProperty("AgeHours", _ageHours);
            this.AddProperty("AgeMinutes", _ageMinutes);
        }

        public override void Reset()
        {
            this.Properties.RemoveMany("AgeHours", "AgeMinutes");
            this.UpdateProperty(nameof(this.Age), this.Age);
            base.Reset();
        }
    }
}
