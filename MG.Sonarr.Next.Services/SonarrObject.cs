using MG.Sonarr.Next.Services.Metadata;
using System.Management.Automation;

namespace MG.Sonarr.Next.Services
{
    public sealed class SonarrObject : PSObject
    {
        MetadataProperty? _meta;

        public object? this[string propertyName]
        {
            get => this.Properties[propertyName]?.Value;
        }
        public MetadataTag Metadata
        {
            get
            {
                _meta ??= MetadataProperty.Empty();
                return _meta.Tag;
            }
            set
            {
                _meta ??= MetadataProperty.Empty();
                _meta.Tag = value;
            }
        }

        public SonarrObject()
            : base(2)
        {
            _meta = MetadataProperty.Empty();
            this.Properties.Add(_meta);
        }
        public SonarrObject(object other)
            : base(other)
        {
            _meta = MetadataProperty.Empty();
            this.Properties.Add(_meta);
        }
    }
}
