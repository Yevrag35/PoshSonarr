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

        protected MetadataProperty MetadataProperty { get; set; }
        public MetadataTag MetadataTag => this.MetadataProperty.Tag;

        protected SonarrObject(int capacity)
            : base(capacity)
        {
            this.MetadataProperty = MetadataProperty.Empty;
            this.Properties.Add(this.MetadataProperty);
        }

        public virtual void Commit()
        {
            PSPropertyInfo? meta = this.Properties[this.MetadataProperty.Name];
            if (meta is null)
            {
                this.Properties.Add(this.MetadataProperty);
            }
        }
        protected abstract MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing);
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
            PSPropertyInfo? meta = this.Properties[this.MetadataProperty.Name];
            if (meta is null)
            {
                this.Properties.Add(this.MetadataProperty);
            }

            return;
        }
        public void SetTag(IMetadataResolver resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            MetadataTag tagToUse = this.GetTag(resolver, this.MetadataProperty.Tag);

            this.MetadataProperty.Tag = tagToUse;
#if !RELEASE
            var prop = (MetadataProperty)this.Properties[this.MetadataProperty.Name];

            Debug.Assert(ReferenceEquals(prop, this.MetadataProperty));
            Debug.Assert(prop.Tag == this.MetadataProperty.Tag);
#endif
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
