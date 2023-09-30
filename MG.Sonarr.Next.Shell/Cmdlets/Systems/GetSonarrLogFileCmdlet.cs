using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.System;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems
{
    [Cmdlet(VerbsCommon.Get, "SonarrLogFile")]
    public sealed class GetSonarrLogFileCmdlet : SonarrMetadataCmdlet
    {
        protected override MetadataTag GetMetadataTag(MetadataResolver resolver)
        {
            return resolver[Meta.LOG_FILE];
        }

        protected override void Process()
        {
            var logs = this.GetAll<LogFileObject>();
            logs.Sort();
            this.WriteCollection(logs);
        }
    }
}
