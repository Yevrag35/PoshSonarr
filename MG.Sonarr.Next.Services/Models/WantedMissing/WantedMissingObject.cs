using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Episodes;

namespace MG.Sonarr.Next.Services.Models.WantedMissing
{
    public sealed class WantedMissingObject : IJsonMetadataTaggable
    {
        public required int Page { get; init; }
        public required int PageSize { get; init; }
        public required int TotalRecords { get; init; }
        public required MetadataList<EpisodeObject> Records { get; init; }

        void IJsonMetadataTaggable.SetTag(MetadataResolver resolver)
        {
            if (this.Records.Count > 0)
            {
                foreach (var rec in this.Records)
                {
                    rec.SetTag(resolver);
                }
            }
        }
    }
}
