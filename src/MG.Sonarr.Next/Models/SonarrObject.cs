using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models
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
        public virtual void Commit()
        {
            return;
        }
        protected abstract MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing);
        public virtual void OnDeserialized()
        {
            return;
        }
        public virtual void Reset()
        {
            return;
        }
        public void SetTag(MetadataResolver resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);

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
        public bool TryGetId(out int id)
        {
            return this.TryGetProperty(Constants.ID, out id);
        }
    }
}
