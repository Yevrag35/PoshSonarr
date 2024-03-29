﻿using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Tags
{
    [SonarrObject]
    public sealed class TagObject : SonarrObject,
        IComparable<TagObject>,
        ISerializableNames<TagObject>
    {
        const int CAPACITY = 3;

        public int Id { get; private set; }
        public string Label { get; private set; } = string.Empty;

        public TagObject()
            : base(CAPACITY)
        {
        }

        public int CompareTo(TagObject? other)
        {
            if (other is null)
            {
                return -1;
            }

            return this.Id.CompareTo(other.Id);
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.TAG];
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();
            if (this.TryGetId(out int id))
            {
                this.Id = id;
            }

            if (this.TryGetProperty(Constants.LABEL, out string? label))
            {
                this.Label = label ?? string.Empty;
            }
        }

        static readonly string _typeName = typeof(TagObject).GetTypeName();
        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
    }
}
