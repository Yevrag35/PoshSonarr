using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Shell.Models;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Services.Models.Series
{
    public class SeriesObject : SonarrObject, IHasId, IEpisodeBySeriesPipeable, IEpisodeFileBySeriesPipeable, IQualityProfilePipeable, ITagPipeable, IJsonOnSerializing
    {
        private const string FIRST_AIRED = "FirstAired";
        private DateOnly _firstAired;
        private SortedSet<int>? _tags;
        private int[]? _originalTags;

        public int Id { get; private set; }
        public int QualityProfileId
        {
            get => this.GetValue<int>(nameof(this.QualityProfileId));
            set => this.SetValue(value);
        }
        int IEpisodeBySeriesPipeable.SeriesId => this.Id;
        int IEpisodeFileBySeriesPipeable.SeriesId => this.Id;
        public SortedSet<int> Tags
        {
            get => _tags ??= new();
            set
            {
                _tags = value;
                _originalTags = new int[value.Count];
                value.CopyTo(_originalTags);
            }
        }
        ISet<int> ITagPipeable.Tags => this.Tags;

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
            this.CommitTags();
        }
        public void CommitTags()
        {
            if (_originalTags is not null)
            {
                Array.Clear(_originalTags);
                if (_originalTags.Length != this.Tags.Count)
                {
                    Array.Resize(ref _originalTags, this.Tags.Count);
                }

                this.Tags.CopyTo(_originalTags);
            }
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.SERIES];
        }

        public override void OnDeserialized()
        {
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

            if (this.TryGetId(out int id))
            {
                this.Id = id;
            }

            if (this.TryGetNonNullProperty(Constants.TAGS, out SortedSet<int>? tags))
            {
                this.Tags = tags;
            }
        }
        public virtual void OnSerializing()
        {
            this.SetValue(_firstAired, FIRST_AIRED);
        }
        public override void Reset()
        {
            _tags?.Clear();
            _tags?.UnionWith(_originalTags ?? Array.Empty<int>());
            this.Properties.Remove(FIRST_AIRED);
        }
    }
}
