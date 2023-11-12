using MG.Sonarr.Next.Collections.Pools;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.PSProperties;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models
{
    /// <summary>
    /// "Dangerous" object.
    /// </summary>
    /// <remarks><inheritdoc cref="PSObject"/></remarks>
    public abstract class SonarrObject : PSObject,
        IJsonSonarrMetadata,
        IJsonOnDeserialized,
        IResettable
    {
        bool _addedType;
        static readonly string _typeName = typeof(SonarrObject).GetName();

        public object? this[string propertyName]
        {
            get => this.Properties[propertyName]?.Value;
        }

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
            this.OnDeserialized(_addedType);
            if (_addedType)
            {
                return;
            }

            this.SetPSTypeName();
            _addedType = true;
        }
        protected virtual void OnDeserialized(bool alreadyCalled)
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
            if (this.DisregardMetadataTag)
            {
                return;
            }

            MetadataTag tagToUse = this.GetTag(resolver, MetadataTag.Empty);
            this.AddOrUpdate(MetadataProperty.Empty.Name, replaceReadOnly: true, tagToUse, static (name, tag) => new MetadataProperty(tag));
        }
        protected virtual void SetPSTypeName()
        {
            this.TypeNames.Insert(0, _typeName);
        }
        public bool TryGetId(out int id)
        {
            return this.TryGetProperty(Constants.ID, out id);
        }
        /// <inheritdoc/>
        bool IResettable.TryReset()
        {
            this.Reset();
            return true;
        }
    }
}
