using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Models.Config;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.NamingConfig;

/// <summary>
/// Represents the <c>Update-SonarrNamingConfig</c> cmdlet, which saves changes to Sonarr's episode naming settings.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes a <see cref="NamingConfigObject"/>, such as the one that <c>Get-SonarrNamingConfig</c> writes with
/// its properties changed, from the pipeline or from <c>-InputObject</c>, and sends Sonarr a request that replaces the
/// stored settings with it. It writes nothing to the pipeline.
/// </para>
/// <para>
/// Serializing the settings for the request can change them, so after the request the cmdlet brings them back in
/// step: it commits them when the update succeeds, and resets them and writes the error when it fails.
/// </para>
/// <para>
/// The cmdlet confirms before it updates the settings, and its confirm impact is low, so it prompts only when
/// <c>-Confirm</c> or <c>$ConfirmPreference</c> asks it to.
/// </para>
/// </remarks>
[Cmdlet(VerbsData.Update, "SonarrNamingConfig", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
public sealed class UpdateSonarrNamingConfigCmdlet : SonarrApiCmdletBase
{
	/// <summary>
	/// Gets or sets the naming settings to send to Sonarr.
	/// </summary>
	/// <remarks>
	/// The settings' ID has to be greater than zero.
	/// </remarks>
	/// <value>The changed settings.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
	[ValidateNotNull]
	[ValidateId(ValidateRangeKind.Positive)]
	public NamingConfigObject InputObject { get; set; } = null!;

	/// <summary>
	/// Sends the settings in <c>-InputObject</c> to Sonarr once the action is confirmed, then commits them when the
	/// request succeeds, or resets them and writes the request's error when it fails.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		var tag = provider.GetMetadataTag(Meta.NamingConfig);
		string url = tag.GetUrlForId(this.InputObject.Id);

		if (!this.ShouldProcess(url, "Update Naming Config"))
		{
			return;
		}

		var response = this.SendPutRequest(url, this.InputObject);
		_ = this.TryCommitFromResponse(this.InputObject, response);
	}
}

