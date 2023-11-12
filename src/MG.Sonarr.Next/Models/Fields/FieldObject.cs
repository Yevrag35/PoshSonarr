using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.PSProperties;

namespace MG.Sonarr.Next.Models.Fields
{
    [SonarrObject]
    public sealed class FieldObject : SonarrObject,
        IComparable<FieldObject>,
        ISerializableNames<FieldObject>
    {
        const int CAPACITY = 8;
        static readonly string _typeName = typeof(FieldObject).GetTypeName();
        protected override bool DisregardMetadataTag => true;
        public int Order { get; private set; }
        public IReadOnlyList<SelectOptionObject> SelectOptions { get; private set; }

        public FieldObject()
            : base(CAPACITY)
        {
            this.SelectOptions = Array.Empty<SelectOptionObject>();
        }

        public int CompareTo(FieldObject? other)
        {
            return Comparer<int?>.Default.Compare(this.Order, other?.Order);
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return existing;
        }

        protected override void OnDeserialized(bool alreadyCalled)
        {
            base.OnDeserialized(alreadyCalled);
            if (this.TryGetProperty(nameof(this.Order), out int order))
            {
                this.Order = order;
            }

            if (this.TryGetNonNullProperty(nameof(this.SelectOptions), out IReadOnlyList<SelectOptionObject>? list))
            {
                this.SelectOptions = list;
            }
            else
            {
                this.Properties.Add(new ReadOnlyCollectionProperty<SelectOptionObject>(nameof(this.SelectOptions), null));
            }
        }

        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
    }
}

