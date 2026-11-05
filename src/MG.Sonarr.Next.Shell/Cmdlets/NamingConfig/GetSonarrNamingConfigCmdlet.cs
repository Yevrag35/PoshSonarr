using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Models.Config;

namespace MG.Sonarr.Next.Shell.Cmdlets.NamingConfig;

[Cmdlet(VerbsCommon.Get, "SonarrNamingConfig")]
public sealed class GetSonarrNamingConfigCmdlet : SonarrApiCmdletBase
{
	protected override void Process(IServiceProvider provider)
	{
		var tag = provider.GetMetadataTag(Meta.NamingConfig);
		var response = this.SendGetRequest<NamingConfigObject>(tag.UrlBase);

		_ = this.TryWriteObject(response);
	}
}

