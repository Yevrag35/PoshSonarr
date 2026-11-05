using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Models.System;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Server;

[Cmdlet(VerbsCommon.Get, "SonarrStatus")]
public sealed class GetSonarrStatusCmdlet : SonarrApiCmdletBase
{
	protected override void Process(IServiceProvider provider)
	{
		var tag = provider.GetMetadataTag(Meta.Status);

		var result = this.SendGetRequest<SystemStatusObject>(tag.UrlBase);
		if (result.IsError)
		{
			this.StopCmdlet(result.Error);
			return;
		}
		else
		{
			this.WriteObject(result.Value);
		}
	}
}
