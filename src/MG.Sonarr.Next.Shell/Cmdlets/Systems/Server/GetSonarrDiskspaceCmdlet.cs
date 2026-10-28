using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Server;

[Cmdlet(VerbsCommon.Get, "SonarrDiskspace")]
public sealed class GetSonarrDiskspaceCmdlet : SonarrApiCmdletBase
{
	protected override void Process(IServiceProvider provider)
	{
		ArgumentNullException.ThrowIfNull(provider);
		var response = this.SendGetRequest<MetadataList<DiskspaceObject>>(Constants.DISKSPACE);
		if (response.IsError)
		{
			this.WriteError(response.Error);
			return;
		}

		this.WriteCollection(response.Value);
	}
}
