using MG.Sonarr.Next.Services.Json;

namespace MG.Sonarr.Next.Services.Metadata
{
    public sealed class MetadataList<T> : List<T>, IJsonMetadataTaggable where T : IJsonMetadataTaggable
    {
        public MetadataList()
            : base()
        {
        }
        public MetadataList(int capacity)
            : base(capacity)
        {
        }
        public MetadataList(IEnumerable<T> items)
            : base(items)
        {
        }

        public void SetTag(MetadataResolver resolver)
        {
            if (this.Count <= 0)
            {
                return;
            }

            foreach (T item in this)
            {
                item.SetTag(resolver);
            }
        }
    }
}
