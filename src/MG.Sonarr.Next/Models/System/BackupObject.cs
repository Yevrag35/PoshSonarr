using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.System
{
    public sealed class BackupObject : SonarrObject
    {
        const int CAPACITY = 6;

        public Uri BackupUri { get; private set; } = null!;
        public string Name { get; private set; } = string.Empty;

        public BackupObject()
            : base(CAPACITY)
        {
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.BACKUP];
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();
            if (this.TryGetNonNullProperty(nameof(this.Name), out string? name))
            {
                this.Name = name;
            }

            if (this.TryGetNonNullProperty(nameof(this.BackupUri), out string? possible)
                &&
                Uri.TryCreate(possible, UriKind.Relative, out Uri? backupUri))
            {
                this.BackupUri = backupUri;
            }
        }
    }
}
