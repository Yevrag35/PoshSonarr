using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.System;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Server
{
    [Cmdlet(VerbsCommon.Get, "SonarrDiskspace")]
    public sealed class GetSonarrDiskspaceCmdlet : SonarrApiCmdletBase
    {
        const string TYPE_NAME = "MG.Sonarr.Next.Models.System.Diskspace";

        protected override void Process(IServiceProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);
            var response = this.SendGetRequest<MetadataList<DiskspaceObject>>(Constants.DISKSPACE);
            if (response.IsError)
            {
                this.WriteError(response.Error);
                return;
            }

            foreach (var pso in response.Data)
            {
                pso.TypeNames.Insert(0, TYPE_NAME);
                this.WriteObject(pso);
            }
        }
    }
}
