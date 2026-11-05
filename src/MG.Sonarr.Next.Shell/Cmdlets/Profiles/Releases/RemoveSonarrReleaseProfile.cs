using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Releases;

/// <summary>
/// Represents the <c>Remove-SonarrReleaseProfile</c> cmdlet, which deletes release profiles from Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes a profile's ID from <c>-Id</c>, or the profiles themselves from the pipeline as
/// <see cref="ReleaseProfileObject"/> instances, such as those that <c>Get-SonarrReleaseProfile</c> writes, and sends
/// Sonarr a delete request for each. It writes nothing to the pipeline, and a verbose message for each profile that it
/// deletes. A failed deletion writes its error, and the cmdlet goes on to the next input.
/// </para>
/// <para>
/// The cmdlet confirms each deletion, and its confirm impact is high, so it prompts by default. <c>-Force</c> skips the
/// prompt, unless <c>-Confirm</c> is given, and in What if mode the cmdlet deletes nothing.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Remove, "SonarrReleaseProfile", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true,
	DefaultParameterSetName = PSConstants.PSetExplicitId)]
[MetadataCanPipe(Tag = Meta.ReleaseProfile)]
public sealed class RemoveSonarrReleaseProfile : SonarrApiCmdletBase
{
	MetadataTag _tag = null!;

	/// <summary>
	/// Gets or sets the ID of the release profile to delete.
	/// </summary>
	/// <remarks>
	/// The ID has to be greater than zero. A profile bound to <see cref="InputObject"/> sets it to the profile's ID.
	/// </remarks>
	/// <value>The profile's ID.</value>
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets the release profile to delete, from the pipeline.
	/// </summary>
	/// <remarks>
	/// The profile's ID has to be greater than zero. Setting the property sets <see cref="Id"/> to the profile's ID, and
	/// the property keeps no profile of its own.
	/// </remarks>
	/// <value>Always <see langword="null"/> when read.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSetPipeline, ValueFromPipeline = true)]
	[ValidateId(ValidateRangeKind.Positive)]
	public ReleaseProfileObject InputObject
	{
		get => null!;
		set => this.Id = value?.Id ?? -1;
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet deletes each profile without prompting for confirmation.
	/// </summary>
	/// <remarks>
	/// The switch skips only the prompt: <c>-Confirm</c> still prompts, and <c>-WhatIf</c> still deletes nothing.
	/// </remarks>
	/// <value>The switch, which is present to skip the confirmation prompt.</value>
	[Parameter]
	public SwitchParameter Force { get; set; }

	/// <summary>
	/// Runs the base class's setup for the scope, then gets the metadata tag of the release profile endpoint.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_tag = provider.GetMetadataTag(Meta.ReleaseProfile);
	}

	/// <summary>
	/// Deletes the release profile with the ID in <c>-Id</c>, or the piped profile's ID, once the action is confirmed,
	/// and writes a verbose message when it succeeds or the request's error when it fails.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		string url = _tag.GetUrlForId(this.Id);

		if (!this.ShouldProcessWithForce(url, "Deleting Release Profile", this.Force))
		{
			return;
		}

		var response = this.SendDeleteRequest(url);
		if (response.IsError)
		{
			this.WriteError(response.Error);
			return;
		}

		this.WriteVerbose($"Deleted Release Profile -> {this.Id}");
	}
}

