using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Shell.Models;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Services.Models.Series
{
    public class SeriesObject : SonarrObject, IHasId, IEpisodeBySeriesPipeable, IEpisodeFileBySeriesPipeable, ITagPipeable, IJsonOnSerializing
    {
        private DateOnly _firstAired;

        public int Id { get; private set; }
        int IEpisodeBySeriesPipeable.SeriesId => this.Id;
        int IEpisodeFileBySeriesPipeable.SeriesId => this.Id;
        public SortedSet<int> Tags { get; private set; } = null!;
        ISet<int> ITagPipeable.Tags => this.Tags;

        public SeriesObject()
            : this(46)
        {
        }

        protected SeriesObject(int capacity)
            : base(capacity)
        {
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.SERIES];
        }

        public override void OnDeserialized()
        {
            PSPropertyInfo? property = this.Properties["FirstAired"];
            if (property is not null && property.Value is DateOnly dateOnly)
            {
                _firstAired = dateOnly;
                this.Properties.Remove(property.Name);
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
            this.AddProperty("FirstAired", _firstAired);
        }
    }
}
