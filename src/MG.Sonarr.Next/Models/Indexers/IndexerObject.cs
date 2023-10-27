using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.DownloadClients;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Indexers
{
    [SonarrObject]
    public sealed class IndexerObject : TagUpdateObject<IndexerObject>,
        IJsonOnSerializing,
        ISerializableNames<IndexerObject>,
        ITestPipeable
    {
        const int CAPACITY = 17;
        static readonly string _typeName = typeof(IndexerObject).GetTypeName();

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
        public string Name { get; private set; } = string.Empty;
        public int Priority
        {
            get => this.GetValue<int>();
            set => this.SetValue(value);
        }
        public string Protocol { get; private set; } = string.Empty;

        public IndexerObject()
            : base(CAPACITY)
        {
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.INDEXER];
        }

        public override void Commit()
        {
            base.Commit();
            this.Name = this.GetValue<string>() ?? string.Empty;
            this.Protocol = this.GetValue<string>() ?? string.Empty;
        }
        public override void OnDeserialized()
        {
            base.OnDeserialized();
            if (this.TryGetNonNullProperty(nameof(this.Name), out string? name))
            {
                this.Name = name;
            }

            if (this.TryGetNonNullProperty(nameof(this.Protocol), out string? pro))
            {
                this.Protocol = pro;
            }
        }
        public void OnSerializing()
        {
            int priority = this.Priority;
            if (priority < 0 || priority > 100)
            {
                this.Priority = 25;
            }

            string? name = this.GetValue<string>(nameof(this.Name));
            if (string.IsNullOrWhiteSpace(name))
            {
                this.UpdateProperty(nameof(this.Name), this.Name);
            }
        }
        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
    }
}
