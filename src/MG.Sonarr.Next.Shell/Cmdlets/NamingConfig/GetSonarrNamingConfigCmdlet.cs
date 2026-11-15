using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Models.Config;

namespace MG.Sonarr.Next.Shell.Cmdlets.NamingConfig;

/// <summary>
/// Represents the <c>Get-SonarrNamingConfig</c> cmdlet, which gets Sonarr's episode naming settings.
/// </summary>
/// <remarks>
/// The cmdlet writes the settings to the pipeline as a <see cref="NamingConfigObject"/>, which
/// <c>Update-SonarrNamingConfig</c> takes back once its properties are changed. It takes no pipeline input.
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrNamingConfig")]
public sealed class GetSonarrNamingConfigCmdlet : SonarrApiCmdletBase
{
	/// <summary>
	/// Gets the naming settings and writes them to the pipeline.
	/// </summary>
	/// <remarks>
	/// When the request fails, the cmdlet writes its error, unless the error is ignorable.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		var tag = provider.GetMetadataTag(Meta.NamingConfig);
		var response = this.SendGetRequest<NamingConfigObject>(tag.UrlBase);

		_ = this.TryWriteObject(response);
	}
}

