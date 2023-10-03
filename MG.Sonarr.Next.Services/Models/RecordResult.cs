﻿using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Metadata;

namespace MG.Sonarr.Next.Services.Models
{
    public class RecordResult<T> : IJsonMetadataTaggable
        where T : IJsonMetadataTaggable
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