﻿using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Metadata;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Services.Models
{
    /// <summary>
    /// Dangerous object.
    /// </summary>
    public abstract class SonarrObject : PSObject, IJsonSonarrMetadata, IJsonOnDeserialized
    {
        public MetadataTag MetadataTag { get; private set; }

        protected SonarrObject(int capacity)
            : base(capacity)
        {
            this.MetadataTag = MetadataTag.Empty;
        }

        public bool TryGetId(out int id)
        {
            return this.TryGetProperty(Constants.ID, out id);
        }

        protected abstract MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing);
        public virtual void OnDeserialized()
        {
        }
        public void SetTag(MetadataResolver resolver)
        {
            this.MetadataTag = this.GetTag(resolver, this.MetadataTag);
            PSPropertyInfo? prop = this.Properties[Constants.META_PROPERTY_NAME];
            if (prop is not null)
            {
                prop.Value = this.MetadataTag;
            }
            else
            {
                this.Properties.Add(new PSNoteProperty(Constants.META_PROPERTY_NAME, this.MetadataTag));
            }
        }
    }
}
