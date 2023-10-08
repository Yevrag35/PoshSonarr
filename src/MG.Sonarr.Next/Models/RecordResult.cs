using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models
{
    public class RecordResult<T> : IJsonMetadataTaggable
        where T : IComparable<T>, IJsonMetadataTaggable
    {
        public required int Page { get; init; }
        public required int PageSize { get; init; }
        public required int TotalRecords { get; init; }
        public required MetadataList<T> Records { get; init; }

        void IJsonMetadataTaggable.SetTag(MetadataResolver resolver)
        {
            this.Records.SetTag(resolver);
        }
    }
}
