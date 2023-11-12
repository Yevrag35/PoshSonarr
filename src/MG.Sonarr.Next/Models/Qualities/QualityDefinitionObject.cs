using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Qualities
{
    [SonarrObject]
    public sealed class QualityDefinitionObject : SonarrObject,
        IComparable<QualityDefinitionObject>,
        ISerializableNames<QualityDefinitionObject>
    {
        const int CAPACITY = 10;
        static readonly string _typeName = typeof(QualityDefinitionObject).GetTypeName();

        public int Id { get; private set; }
        public string Title
        {
            get => this.GetStringOrEmpty();
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

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.QUALITY_DEFINITION];
        }
        public override void OnDeserialized()
        {
            base.OnDeserialized();
            if (this.TryGetId(out int id))
            {
                this.Id = id;
            }
        }
        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
    }
}
