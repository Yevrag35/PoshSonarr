using MG.Sonarr.Next.Models.Config;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.NamingConfig
{
    [Cmdlet(VerbsCommon.Get, "SonarrNamingConfig")]
    public sealed class GetSonarrNamingConfigCmdlet : SonarrApiCmdletBase
    {
        protected override void Process(IServiceProvider provider)
        {
            var tag = provider.GetMetadataTag(Meta.NAMING_CONFIG);
            var response = this.SendGetRequest<NamingConfigObject>(tag.UrlBase);

            _ = this.TryWriteObject(in response);
        }
    }
}

