using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models
{
    public sealed class RecordResult<T> : IJsonMetadataTaggable, ISortable
        where T : IComparable<T>, IJsonMetadataTaggable
    {
        int ISortable.Count => this.Records.Count;
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalRecords { get; init; }
        public required MetadataList<T> Records { get; init; }

        public void SetTag(IMetadataResolver resolver)
        {
            this.Records.SetTag(resolver);
        }
        void ISortable.Sort()
        {
            this.Records.Sort();
        }
    }
}
