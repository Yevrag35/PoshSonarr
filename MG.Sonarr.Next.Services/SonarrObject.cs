using MG.Sonarr.Next.Services.Metadata;
using System.Management.Automation;

namespace MG.Sonarr.Next.Services
{
    public sealed class SonarrObject : PSObject
    {
        public object? this[string propertyName]
        {
            get => this.Properties[propertyName]?.Value;
        } 
        public MetadataTag Metadata { get; set; }

        public SonarrObject()
            : base(2)
        {
            this.Metadata = MetadataTag.Empty;
        }
        public SonarrObject(object other)
            : base(other)
        {
            this.Metadata = MetadataTag.Empty;
        }
    }
}
