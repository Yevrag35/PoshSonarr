using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Services.Models.Releases
{
    public sealed class ReleaseObject : SonarrObject, IJsonOnSerializing
    {
        int _age;
        double _ageHours;
        double _ageMinutes;

        public TimeSpan Age { get; private set; }

        public ReleaseObject() : base(45)
        {
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.RELEASE];
        }

        public override void OnDeserialized()
        {
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

            this.Age = TimeSpan.FromMinutes(_ageMinutes);
            this.Reset();
        }

        public void OnSerializing()
        {
            this.UpdateProperty(nameof(this.Age), _age);
            this.AddProperty("AgeHours", _ageHours);
            this.AddProperty("AgeMinutes", _ageMinutes);
        }

        public override void Reset()
        {
            this.Properties.RemoveMany("AgeHours", "AgeMinutes");
            this.UpdateProperty(nameof(this.Age), this.Age);
        }
    }
}
