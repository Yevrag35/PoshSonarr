using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Series
{
    [SonarrObject]
    public class SeriesObject : TagUpdateObject<SeriesObject>,
        IEpisodeBySeriesPipeable,
        IEpisodeFileBySeriesPipeable,
        IHasId,
        IJsonOnSerializing,
        ILanguageProfilePipeable,
        IQualityProfilePipeable,
        IReleasePipeableBySeries,
        ISerializableNames<SeriesObject>,
        ISeriesPipeable
    {
        private const string FIRST_AIRED = "FirstAired";
        static readonly string _typeName = typeof(SeriesObject).GetTypeName();
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
        int IEpisodeBySeriesPipeable.SeriesId => this.SeriesId;
        int IEpisodeFileBySeriesPipeable.SeriesId => this.SeriesId;
        int IReleasePipeableBySeries.SeriesId => this.SeriesId;
        int ISeriesPipeable.SeriesId => this.SeriesId;
        protected int SeriesId => this.Id;
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

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
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
        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }

        const int DICT_CAPACITY = 2;
        protected private static readonly Lazy<JsonNameHolder> _names = new(GetJsonNames);

        private static JsonNameHolder GetJsonNames()
        {
            return JsonNameHolder
                .FromDeserializationNamePairs(new KeyValuePair<string, string>[]
                {
                    new("ImdbId", "IMDbId"),
                    new("SeasonFolder", "UseSeasonFolders"),
                });
        }

        public static IReadOnlyDictionary<string, string> GetDeserializedNames()
        {
            return _names.Value.DeserializationNames;
        }
        public static IReadOnlyDictionary<string, string> GetSerializedNames()
        {
            return _names.Value.SerializationNames;
        }
    }
}
