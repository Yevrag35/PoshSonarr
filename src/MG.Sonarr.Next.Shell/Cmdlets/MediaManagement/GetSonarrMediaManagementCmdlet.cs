using MG.Sonarr.Next.Models.Media;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.MediaManagement
{
    [Cmdlet(VerbsCommon.Get, "SonarrMediaManagement")]
    public sealed class GetSonarrMediaManagementCmdlet : SonarrApiCmdletBase
    {
        protected override void Process(IServiceProvider provider)
        {
            var tag = provider.GetMetadataTag(Meta.MEDIA_MANGEMENT);
            var response = this.SendGetRequest<MediaManagementObject>(tag.UrlBase);

            _ = this.TryWriteObject(in response);
        }
    }
}
