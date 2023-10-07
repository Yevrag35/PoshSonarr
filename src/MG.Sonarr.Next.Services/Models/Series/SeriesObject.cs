using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Shell.Models;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Services.Models.Series
{
    public class SeriesObject : TagUpdateObject,
        IEpisodeBySeriesPipeable,
        IEpisodeFileBySeriesPipeable,
        IHasId,
        ILanguageProfilePipeable,
        IQualityProfilePipeable,
        IReleasePipeableBySeries,
        IJsonOnSerializing
    {
        private const string FIRST_AIRED = "FirstAired";
        private DateOnly _firstAired;

        public int LanguageProfileId => this.GetValue<int>();
        public int QualityProfileId
        {
            get => this.GetValue<int>();
            set => this.SetValue(value);
        }
        int IEpisodeBySeriesPipeable.SeriesId => this.Id;
        int IEpisodeFileBySeriesPipeable.SeriesId => this.Id;
        int IReleasePipeableBySeries.SeriesId => this.Id;
        public virtual string Title { get; set; } = string.Empty;

        public SeriesObject()
            : this(46)
        {
        }

        protected SeriesObject(int capacity)
            : base(capacity)
        {
        }

        public override void Commit()
        {
            this.Properties.Remove(FIRST_AIRED);
            base.Commit();
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.SERIES];
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();

            PSPropertyInfo? property = this.Properties[FIRST_AIRED];
            if (property is not null && property.Value is DateOnly dateOnly)
            {
                _firstAired = dateOnly;
                this.Properties.Remove(property.Name);
            }

            if (this.TryGetProperty(nameof(this.QualityProfileId), out int profileId))
            {
                this.QualityProfileId = profileId;
            }

            if (this.TryGetNonNullProperty(nameof(this.Title), out string? title))
            {
                this.Title = title;
            }
        }
        public virtual void OnSerializing()
        {
            this.SetValue(_firstAired, FIRST_AIRED);
        }
        public override void Reset()
        {
            this.Properties.Remove(FIRST_AIRED);
            base.Reset();
        }
    }
}
