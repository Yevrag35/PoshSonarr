using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.System;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems
{
    [Cmdlet(VerbsCommon.Get, "SonarrBackup")]
    public sealed class GetSonarrBackupCmdlet : SonarrMetadataCmdlet
    {
        public GetSonarrBackupCmdlet() : base()
        {
        }

        protected override MetadataTag GetMetadataTag(MetadataResolver resolver)
        {
            return resolver[Meta.BACKUP];
        }

        protected override void Process()
        {
            var response = this.GetAll<BackupObject>();
            this.WriteCollection(response);
        }
    }
}
