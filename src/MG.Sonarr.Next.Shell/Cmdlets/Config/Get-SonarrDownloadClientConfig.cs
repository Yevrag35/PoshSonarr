using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Config;

namespace MG.Sonarr.Next.Shell.Cmdlets.Config;

[Cmdlet(VerbsCommon.Get, "SonarrDownloadClientConfig")]
public sealed class GetSonarrDownloadClientConfig : SonarrApiCmdletBase
{
	protected override void Process(IServiceProvider provider)
	{
		MetadataTag tag = provider.GetMetadataTag(Meta.DownloadClientConfig);
		var response = this.SendGetRequest<DownloadClientConfigObject>(tag.UrlBase);
		this.TryWriteObject(response);
	}
}