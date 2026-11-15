using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Exceptions.Shell;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Delay;

/// <summary>
/// Represents the <c>Remove-SonarrDelayProfile</c> cmdlet, which deletes delay profiles from Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes the profiles' IDs from <c>-Id</c>, or the profiles themselves from the pipeline as
/// <see cref="DelayProfileObject"/> instances, such as those that <c>Get-SonarrDelayProfile</c> writes. It collects the
/// IDs from every input, and when the input ends, it sends Sonarr a delete request for each one, in ascending order. It
/// writes nothing to the pipeline, and a verbose message for each profile that it deletes.
/// </para>
/// <para>
/// Sonarr's built-in delay profile, whose ID is 1, can't be deleted. <c>-Id</c> refuses that ID when it binds, and for a
/// piped profile with that ID, the cmdlet writes an error and deletes the others.
/// </para>
/// <para>
/// The cmdlet confirms each deletion, and its confirm impact is high, so it prompts by default. <c>-Force</c> skips the
/// prompt, unless <c>-Confirm</c> is given, and in What if mode the cmdlet deletes nothing.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Remove, "SonarrDelayProfile", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
[MetadataCanPipe(Tag = Meta.DelayProfile)]
public sealed class RemoveSonarrDelayProfileCmdlet : SonarrMetadataCmdlet
{
	private SortedSet<int> _ids = null!;

	/// <summary>
	/// Gets or sets the IDs of the delay profiles to delete.
	/// </summary>
	/// <remarks>
	/// Each ID has to be at least 2, since the profile with the ID 1 is Sonarr's built-in one. The parameter can't be
	/// combined with pipeline input.
	/// </remarks>
	/// <value>The IDs of the profiles.</value>
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(2, int.MaxValue)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets the delay profiles to delete, from the pipeline.
	/// </summary>
	/// <remarks>
	/// Each profile's ID has to be greater than zero.
	/// </remarks>
	/// <value>The profiles whose IDs the cmdlet deletes.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline)]
	[ValidateIds(ValidateRangeKind.Positive)]
	public DelayProfileObject[] InputObject { get; set; } = [];

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
	/// Gets the metadata tag of the delay profile endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The delay profile's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.DelayProfile];
	}
	/// <summary>
	/// Runs the base class's setup for the scope, then creates the set of IDs that the cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = [];
	}

	/// <summary>
	/// Adds the IDs in <c>-Id</c> to the set of IDs to delete.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
	}
	/// <summary>
	/// Adds the IDs of the piped profiles to the set of IDs to delete.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Process(IServiceProvider provider)
	{
		if (this.InputObject.Length > 0)
		{
			_ids.UnionWith(this.InputObject.Select(x => x.Id));
		}
	}
	/// <summary>
	/// Deletes the profile with each collected ID, after writing an error for the built-in profile's ID when it was
	/// collected and leaving that ID out.
	/// </summary>
	/// <remarks>
	/// A failed deletion writes its error, and the cmdlet goes on to the next ID.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in nameof()")]
	protected override void End(IServiceProvider provider)
	{
		if (_ids.Count == 0)
		{
			return;
		}
		else if (_ids.Contains(1))
		{
			this.WriteError(new ErrorRecord(new SonarrParameterException(nameof(Id), ParameterErrorType.Invalid, "Cannot delete a built-in Sonarr object."), "SonarrParameterException.DeletingRestrictedId", ErrorCategory.InvalidArgument, null));
			_ids.Remove(1);
		}

		bool force = this.Force.ToBool();
		foreach (int id in _ids)
		{
			this.DeleteProfile(in id, this.Tag, force);
		}
	}

	/// <summary>
	/// Deletes the delay profile with the specified ID once the action is confirmed, and writes a verbose message when it
	/// succeeds or the request's error when it fails.
	/// </summary>
	/// <param name="id">The ID of the profile to delete.</param>
	/// <param name="tag">The metadata tag of the delay profile endpoint.</param>
	/// <param name="force">
	/// <see langword="true"/> to skip the confirmation prompt, as <c>-Force</c> does; otherwise, <see langword="false"/>.
	/// </param>
	private void DeleteProfile(in int id, MetadataTag tag, bool force)
	{
		string url = tag.GetUrlForId(id);
		if (!this.ShouldProcessWithForce(url, "Delete Delay Profile", force))
		{
			return;
		}

		var response = this.SendDeleteRequest(url);
		if (response.IsError)
		{
			this.WriteError(response.Error);
			return;
		}

		this.WriteVerbose($"Deleted Delay Profile -> {id}");
	}
}

