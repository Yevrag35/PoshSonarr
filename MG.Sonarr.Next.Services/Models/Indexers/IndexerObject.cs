using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;

namespace MG.Sonarr.Next.Services.Models.Indexers
{
    public sealed class IndexerObject : TagUpdateObject
    {
        public bool EnableRss
        {
            get => this.GetValue<bool>();
        }
        public bool EnableAutomaticSearch
        {
            get => this.GetValue<bool>();
        }
        public bool EnableInteractiveSearch
        {
            get => this.GetValue<bool>();
        }
        public string Name
        {
            get => this.GetValue<string>() ?? string.Empty;
            set => this.SetValue(value);
        }
        public int Priority
        {
            get => this.GetValue<int>();
        }
        public string Protocol
        {
            get => this.GetValue<string>() ?? string.Empty;
        }

        public IndexerObject() : base(17)
        {
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.INDEXER];
        }
    }
}
