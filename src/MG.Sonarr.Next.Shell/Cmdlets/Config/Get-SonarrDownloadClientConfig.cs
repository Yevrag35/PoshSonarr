using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Config;

namespace MG.Sonarr.Next.Shell.Cmdlets.Config;

/// <summary>
/// Represents the <c>Get-SonarrDownloadClientConfig</c> cmdlet, which gets Sonarr's download client settings.
/// </summary>
/// <remarks>
/// The cmdlet writes the settings that apply to every download client to the pipeline as a
/// <see cref="DownloadClientConfigObject"/>. It takes no pipeline input.
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrDownloadClientConfig")]
public sealed class GetSonarrDownloadClientConfig : SonarrApiCmdletBase
{
	/// <summary>
	/// Gets the download client settings and writes them to the pipeline.
	/// </summary>
	/// <remarks>
	/// When the request fails, the cmdlet writes its error, unless the error is ignorable.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		MetadataTag tag = provider.GetMetadataTag(Meta.DownloadClientConfig);
		var response = this.SendGetRequest<DownloadClientConfigObject>(tag.UrlBase);
		this.TryWriteObject(response);
	}
}