using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Models.Media;

namespace MG.Sonarr.Next.Shell.Cmdlets.MediaManagement;

/// <summary>
/// Represents the <c>Get-SonarrMediaManagement</c> cmdlet, which gets Sonarr's media management settings.
/// </summary>
/// <remarks>
/// The cmdlet writes the settings to the pipeline as a <see cref="MediaManagementObject"/>, which
/// <c>Update-SonarrMediaManagement</c> takes back once its properties are changed. It takes no pipeline input.
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrMediaManagement")]
public sealed class GetSonarrMediaManagementCmdlet : SonarrApiCmdletBase
{
	/// <summary>
	/// Gets the media management settings and writes them to the pipeline.
	/// </summary>
	/// <remarks>
	/// When the request fails, the cmdlet writes its error, unless the error is ignorable.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		var tag = provider.GetMetadataTag(Meta.MediaManagement);
		var response = this.SendGetRequest<MediaManagementObject>(tag.UrlBase);

		_ = this.TryWriteObject(response);
	}
}
