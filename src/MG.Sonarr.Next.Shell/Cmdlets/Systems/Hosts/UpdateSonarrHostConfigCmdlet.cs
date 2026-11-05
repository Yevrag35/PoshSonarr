using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Hosts;

/// <summary>
/// Represents the <c>Update-SonarrHostConfig</c> cmdlet, which saves changed host settings to Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes the settings from the pipeline or from <c>-InputObject</c>, such as an object that
/// <c>Get-SonarrHostConfig</c> wrote and that the caller changed, and sends them to <c>/config/host</c> with a
/// <c>PUT</c>. Settings that <c>Get-SonarrHostConfig</c> wrote without their sensitive values still send the API key
/// and the passwords that Sonarr gave, so the update keeps them. The cmdlet writes nothing to the pipeline.
/// </para>
/// <para>
/// The cmdlet confirms before it sends the settings, as <c>ShouldProcess</c> does at a low confirm impact, and in What
/// if mode it shows its What if line and sends nothing. When the request fails, the cmdlet writes its error.
/// </para>
/// <para>
/// The cmdlet's aliases are <c>Set-SonarrHostConfig</c>, <c>Update-SonarrHost</c>, and <c>Set-SonarrHost</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsData.Update, "SonarrHostConfig", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
[Alias("Set-SonarrHostConfig", "Update-SonarrHost", "Set-SonarrHost")]
[MetadataCanPipe(Tag = Meta.HostConfig)]
public sealed class UpdateSonarrHostConfigCmdlet : SonarrApiCmdletBase
{
	/// <summary>
	/// Gets or sets the host settings to save.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, such as the object that <c>Get-SonarrHostConfig</c> writes. The settings' ID
	/// has to be greater than zero.
	/// </remarks>
	/// <value>The host settings.</value>
	[Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
	[ValidateNotNull]
	[ValidateId(ValidateRangeKind.Positive)]
	public HostObject InputObject { get; set; } = null!;

	/// <summary>
	/// Sends the host settings to Sonarr, once the user confirms, and brings the settings object back to its state
	/// before the request.
	/// </summary>
	/// <remarks>
	/// Sending the settings writes back values that the object hides, so the cmdlet commits the object after a request
	/// that succeeds and resets it after one that fails, and the object looks as it did before.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		MetadataTag tag = provider.GetMetadataTag(Meta.HostConfig);

		if (this.ShouldProcess(tag.UrlBase, "Update Host Config"))
		{
			var response = this.SendPutRequest(tag.UrlBase, this.InputObject);
			if (response.IsError)
			{
				this.InputObject.Reset();
				this.WriteError(response.Error);
			}
			else
			{
				this.InputObject.Commit();
			}
		}
	}
}
