using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Series
{
    public class SeriesObject : TagUpdateObject,
        IComparable<SeriesObject>,
        IEpisodeBySeriesPipeable,
        IEpisodeFileBySeriesPipeable,
        IHasId,
        IJsonOnSerializing,
        ILanguageProfilePipeable,
        IQualityProfilePipeable,
        IReleasePipeableBySeries,
        ISerializableNames<SeriesObject>
    {
        private const string FIRST_AIRED = "FirstAired";
        private DateOnly _firstAired;

        public int LanguageProfileId
        {
            get => this.GetValue<int>();
            set => this.SetValue(value);
        }
        public int QualityProfileId
        {
            get => this.GetValue<int>();
            set => this.SetValue(value);
        }
        int IEpisodeBySeriesPipeable.SeriesId => this.Id;
        int IEpisodeFileBySeriesPipeable.SeriesId => this.Id;
        int IReleasePipeableBySeries.SeriesId => this.Id;
        public string Title { get; set; } = string.Empty;

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

        public virtual int CompareTo(SeriesObject? other)
        {
            return this.CompareTo((TagUpdateObject?)other); // By ID.
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
                this.Properties.Add(new PSAliasProperty("Name", nameof(this.Title)));
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

        const int DICT_CAPACITY = 2;
        protected private static IReadOnlyDictionary<string, string> GetSerializationNames()
        {
            return new Dictionary<string, string>(DICT_CAPACITY, StringComparer.InvariantCultureIgnoreCase)
            {
                { "ImdbId", "IMDbId" },
                { "SeasonFolder", "UseSeasonFolders" },
            };
        }

        public static IReadOnlyDictionary<string, string> GetDeserializedNames()
        {
            return GetSerializationNames();
        }
        public static IReadOnlyDictionary<string, string> GetSerializedNames()
        {
            return GetDeserializedNames()
                .ToDictionary(x => x.Value, x => x.Key, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
