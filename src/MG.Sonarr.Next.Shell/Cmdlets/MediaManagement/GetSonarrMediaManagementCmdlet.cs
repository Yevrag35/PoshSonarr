using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Models.Media;

namespace MG.Sonarr.Next.Shell.Cmdlets.MediaManagement;

[Cmdlet(VerbsCommon.Get, "SonarrMediaManagement")]
public sealed class GetSonarrMediaManagementCmdlet : SonarrApiCmdletBase
{
	protected override void Process(IServiceProvider provider)
	{
		var tag = provider.GetMetadataTag(Meta.MediaManagement);
		var response = this.SendGetRequest<MediaManagementObject>(tag.UrlBase);

		_ = this.TryWriteObject(response);
	}
}
