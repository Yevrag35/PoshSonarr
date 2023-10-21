using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models
{
    public sealed class RecordResult<T> : IJsonMetadataTaggable
        where T : IComparable<T>, IJsonMetadataTaggable
    {
        public required int Page { get; init; }
        public required int PageSize { get; init; }
        public required int TotalRecords { get; init; }
        public required MetadataList<T> Records { get; init; }

        public void SetTag(IMetadataResolver resolver)
        {
            this.Records.SetTag(resolver);
        }
    }
}
