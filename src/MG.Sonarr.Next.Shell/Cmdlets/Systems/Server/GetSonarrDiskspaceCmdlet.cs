using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Server;

/// <summary>
/// Represents the <c>Get-SonarrDiskspace</c> cmdlet, which gets the free and total space of the drives that Sonarr's
/// server sees.
/// </summary>
/// <remarks>
/// The cmdlet gets the drives from <c>/diskspace</c> and writes each one to the pipeline as a
/// <see cref="DiskspaceObject"/>, sorted by path. When the request fails, the cmdlet writes its error and no drives.
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrDiskspace")]
public sealed class GetSonarrDiskspaceCmdlet : SonarrApiCmdletBase
{
	/// <summary>
	/// Gets the drives' space and writes each drive to the pipeline.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		ArgumentNullException.ThrowIfNull(provider);
		var response = this.SendGetRequest<MetadataList<DiskspaceObject>>(Constants.DiskSpace);
		if (response.IsError)
		{
			this.WriteError(response.Error);
			return;
		}

		this.WriteCollection(response.Value);
	}
}
