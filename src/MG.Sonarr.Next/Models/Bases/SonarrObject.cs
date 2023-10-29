using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.PSProperties;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models
{
    /// <summary>
    /// Dangerous object.
    /// </summary>
    public abstract class SonarrObject : PSObject,
        IJsonSonarrMetadata,
        IJsonOnDeserialized
    {
        bool _addedType;
        static readonly string _typeName = typeof(SonarrObject).GetTypeName();

        protected virtual bool DisregardMetadataTag { get; }
        public MetadataTag MetadataTag => this.GetValue<MetadataTag>() ?? MetadataTag.Empty;

        protected SonarrObject(int capacity)
            : base(capacity)
        {
        }

        public virtual void Commit()
        {
            return;
        }
        protected abstract MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing);
        internal virtual bool ShouldBeReadOnly(string propertyName, Type parentType)
        {
            return true;
        }
        public virtual void OnDeserialized()
        {
            if (_addedType)
            {
                return;
            }

            this.SetPSTypeName();
            _addedType = true;
        }
        public virtual void Reset()
        {
            return;
        }
        public void SetTag(IMetadataResolver resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            if (this.DisregardMetadataTag)
            {
                return;
            }

            MetadataTag tagToUse = this.GetTag(resolver, MetadataTag.Empty);
            this.Properties.Add(new MetadataProperty(tagToUse));
        }
        protected virtual void SetPSTypeName()
        {
            this.TypeNames.Insert(0, _typeName);
        }
        public bool TryGetId(out int id)
        {
            return this.TryGetProperty(Constants.ID, out id);
        }
    }
}
