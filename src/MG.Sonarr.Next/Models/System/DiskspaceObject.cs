using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.PSProperties;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.System
{
    [SonarrObject]
    public sealed class DiskspaceObject : SonarrObject,
        IComparable<DiskspaceObject>,
        ISerializableNames<DiskspaceObject>
    {
        const int CAPACITY = 6;
        const string LABEL = "Label";

        public Size FreeSpace { get; private set; }
        public string Path { get; private set; } = string.Empty;
        public Size TotalSpace { get; private set; }

        public DiskspaceObject()
            : base(CAPACITY)
        {
        }

        public int CompareTo(DiskspaceObject? other)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(this.Path, other?.Path);
        }
        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.DISK];
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();
            PSPropertyInfo? label = this.Properties[LABEL];
            if (label is null)
            {
                this.Properties.Add(new StringNoteProperty(LABEL, string.Empty));
            }

            if (this.TryGetNonNullProperty(nameof(this.Path), out string? path))
            {
                this.Path = path;
            }

            if (this.TryGetProperty(nameof(this.FreeSpace), out long fs))
            {
                this.FreeSpace = new Size(in fs);
            }

            if (this.TryGetProperty(nameof(this.TotalSpace), out long ts))
            {
                this.TotalSpace = new Size(in ts);
            }
        }
    }
}

