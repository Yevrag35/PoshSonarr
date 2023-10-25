using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.PSProperties;
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
        protected abstract MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing);
        public virtual void OnDeserialized()
        {
            return;
        }
        public virtual void Reset()
        {
            return;
        }
        public void SetTag(IMetadataResolver resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);

            this.MetadataTag = this.GetTag(resolver, this.MetadataTag);
            this.Properties.Remove(MetadataResolver.META_PROPERTY_NAME);
            this.Properties.Add(new MetadataProperty(this.MetadataTag));
        }
        public bool TryGetId(out int id)
        {
            return this.TryGetProperty(Constants.ID, out id);
        }
    }
}
