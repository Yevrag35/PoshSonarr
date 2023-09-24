using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Shell.Models;

namespace MG.Sonarr.Next.Services.Models.Series
{
    public class SeriesObject : SonarrObject, IHasId, IEpisodePipeable, ITagPipeable
    {
        public int Id { get; private set; }
        int IEpisodePipeable.SeriesId => this.Id;
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
            if (this.TryGetId(out int id))
            {
                this.Id = id;
            }

            if (this.TryGetNonNullProperty(Constants.TAGS, out SortedSet<int>? tags))
            {
                this.Tags = tags;
            }
        }
    }
}
