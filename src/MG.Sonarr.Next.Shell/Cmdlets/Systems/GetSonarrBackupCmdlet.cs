using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems
{
    [Cmdlet(VerbsCommon.Get, "SonarrBackup")]
    public sealed class GetSonarrBackupCmdlet : SonarrMetadataCmdlet
    {
        protected override int Capacity => 0;

        protected override MetadataTag GetMetadataTag(MetadataResolver resolver)
        {
            return resolver[Meta.BACKUP];
        }

        protected override void Process(IServiceProvider provider)
        {
            var backups = this.GetAll<BackupObject>();
            this.WriteCollection(backups);
        }
    }
}
