using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Models.System;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Server;

/// <summary>
/// Represents the <c>Get-SonarrStatus</c> cmdlet, which gets the status of the Sonarr server.
/// </summary>
/// <remarks>
/// The cmdlet gets the status from <c>/system/status</c>, such as the server's version, operating system, and
/// authentication type, and writes it to the pipeline as a <see cref="SystemStatusObject"/>. When the request fails,
/// the cmdlet stops with its error.
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrStatus")]
public sealed class GetSonarrStatusCmdlet : SonarrApiCmdletBase
{
	/// <summary>
	/// Gets the server's status and writes it to the pipeline.
	/// </summary>
	/// <remarks>
	/// When the request fails, the cmdlet stops with its error, so it processes no later input.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
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
