using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Fields
{
    [SonarrObject]
    public sealed class SelectOptionObject : SonarrObject,
        IComparable<SelectOptionObject>,
        ISerializableNames<SelectOptionObject>
    {
        const int CAPACITY = 3;
        protected override bool DisregardMetadataTag => true;

        public int Order { get; private set; }

        public SelectOptionObject()
            : base(CAPACITY)
        {
        }

        public int CompareTo(SelectOptionObject? other)
        {
            return Comparer<int?>.Default.Compare(this.Order, other?.Order);
        }
        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return existing;
        }

        protected override void OnDeserialized(bool alreadyCalled)
        {
            if (this.TryGetProperty(nameof(this.Order), out int order))
            {
                this.Order = order;
            }
        }
    }
}

