using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Config;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Config
{
    [Cmdlet(VerbsCommon.Get, "SonarrDownloadClientConfig")]
    public sealed class GetSonarrDownloadClientConfig : SonarrApiCmdletBase
    {
        protected override void Process(IServiceProvider provider)
        {
            MetadataTag tag = provider.GetMetadataTag(Meta.DOWNLOAD_CLIENT_CONFIG);
            var response = this.SendGetRequest<DownloadClientConfigObject>(tag.UrlBase);
            this.TryWriteObject(response);
        }
    }
}