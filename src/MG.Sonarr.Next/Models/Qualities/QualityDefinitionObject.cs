using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Qualities
{
    public sealed class QualityDefinitionObject : SonarrObject, IComparable<QualityDefinitionObject>
    {
        const int CAPACITY = 10;

        public int Id { get; private set; }
        public string Title
        {
            get => this.GetValue<string>() ?? string.Empty;
            set => this.SetValue(value);
        }

        public QualityDefinitionObject()
            : base(CAPACITY)
        {
        }

        public int CompareTo(QualityDefinitionObject? other)
        {
            return Comparer<int?>.Default.Compare(this.Id, other?.Id);
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.QUALITY_DEFINITION];
        }

        public override void OnDeserialized()
        {
            if (this.TryGetId(out int id))
            {
                this.Id = id;
            }
        }
    }
}
