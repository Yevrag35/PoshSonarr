using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems
{
    [Cmdlet(VerbsCommon.Get, "SonarrLogFile")]
    public sealed class GetSonarrLogFileCmdlet : SonarrMetadataCmdlet
    {
        protected override int Capacity => 0;

        protected override MetadataTag GetMetadataTag(MetadataResolver resolver)
        {
            return resolver[Meta.LOG_FILE];
        }

        protected override void Process(IServiceProvider provider)
        {
            var logs = this.GetAll<LogFileObject>();
            
            var updateLogs = this.SendGetRequest<MetadataList<LogFileObject>>(this.Tag.UrlBase + "/update");
            if (updateLogs.IsError)
            {
                this.WriteError(updateLogs.Error);
                return;
            }

            logs.AddRange(updateLogs.Data);
            logs.Sort();
            this.WriteCollection(logs);
        }
    }
}
