using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Models.Media;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.MediaManagement;

/// <summary>
/// Represents the <c>Update-SonarrMediaManagement</c> cmdlet, which saves changes to Sonarr's media management
/// settings.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes a <see cref="MediaManagementObject"/>, such as the one that <c>Get-SonarrMediaManagement</c> writes
/// with its properties changed, from the pipeline or from <c>-InputObject</c>, and sends Sonarr a request that replaces
/// the stored settings with it. It writes nothing to the pipeline.
/// </para>
/// <para>
/// The cmdlet sends the recycle bin's path ending with a directory separator: when the settings name a recycle bin
/// whose path doesn't end with one, the cmdlet gets Sonarr's status to learn the server's separator and adds it to the
/// path before sending. When the update fails, the cmdlet puts the path back as it was and writes the error.
/// </para>
/// <para>
/// The cmdlet confirms before it updates the settings, and its confirm impact is high, so it prompts by default.
/// <c>-Force</c> skips the prompt, unless <c>-Confirm</c> is given, and in What if mode the cmdlet changes nothing.
/// </para>
/// </remarks>
[Cmdlet(VerbsData.Update, "SonarrMediaManagement", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
[MetadataCanPipe(Tag = Meta.MediaManagement)]
public sealed class UpdateSonarrMediaManagementCmdlet : SonarrApiCmdletBase
{
	/// <summary>
	/// Gets or sets the media management settings to send to Sonarr.
	/// </summary>
	/// <remarks>
	/// The settings' ID has to be greater than zero.
	/// </remarks>
	/// <value>The changed settings.</value>
	[Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
	[ValidateNotNull]
	[ValidateId(ValidateRangeKind.Positive)]
	public MediaManagementObject InputObject { get; set; } = null!;

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet updates the settings without prompting for confirmation.
	/// </summary>
	/// <remarks>
	/// The switch skips only the prompt: <c>-Confirm</c> still prompts, and <c>-WhatIf</c> still changes nothing.
	/// </remarks>
	/// <value>The switch, which is present to skip the confirmation prompt.</value>
	[Parameter]
	public SwitchParameter Force { get; set; }

	/// <summary>
	/// Sends the settings in <c>-InputObject</c> to Sonarr once the action is confirmed, after adding the server's
	/// directory separator to the end of the recycle bin's path when it lacks one.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When the request for Sonarr's status fails, the cmdlet writes its error and sends no update.
	/// </para>
	/// <para>
	/// When the update succeeds, the cmdlet commits the settings. When it fails, the cmdlet puts the recycle bin's path
	/// back as it was, resets the settings, and writes the error.
	/// </para>
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		var tag = provider.GetMetadataTag(Meta.MediaManagement);
		string url = tag.GetUrlForId(this.InputObject.Id);

		if (!this.ShouldProcessWithForce(url, "Update Media Management Settings", this.Force))
		{
			return;
		}

		string rbPath = this.InputObject.RecycleBinPath;
		if (!string.IsNullOrWhiteSpace(rbPath))
		{
			if (!this.TryGetDirectorySeparator(provider, out char separator))
			{
				return;
			}

			if (!this.InputObject.IsRecycleBinPathProper(separator, out rbPath))
			{
				this.InputObject.RecycleBinPath = rbPath + separator;
			}
		}

		var response = this.SendPutRequest(url, this.InputObject);
		if (response.IsError)
		{
			this.InputObject.RecycleBinPath = rbPath;
			this.InputObject.Reset();
			this.WriteError(response.Error);
		}
		else
		{
			this.InputObject.Commit();
		}
	}

	/// <summary>
	/// Attempts to get the character that separates directories on the Sonarr server, from Sonarr's status.
	/// </summary>
	/// <remarks>
	/// When the request fails, the method writes its error.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	/// <param name="separator">
	/// When the method returns, the server's directory separator, or <see langword="default"/> when the request failed.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the request succeeded; otherwise, <see langword="false"/>.
	/// </returns>
	private bool TryGetDirectorySeparator(IServiceProvider provider, out char separator)
	{
		var tag = provider.GetMetadataTag(Meta.Status);
		var result = this.SendGetRequest<SystemStatusObject>(tag.UrlBase);
		if (result.IsError)
		{
			separator = default;
			this.WriteError(result.Error);
			return false;
		}

		separator = result.Value.DirectorySeparator;
		return true;
	}
}
