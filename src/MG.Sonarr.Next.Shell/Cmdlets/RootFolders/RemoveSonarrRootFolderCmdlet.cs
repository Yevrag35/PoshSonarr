using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.RootFolders;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.RootFolders;

/// <summary>
/// Represents the <c>Remove-SonarrRootFolder</c> cmdlet, which removes root folders from Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes the folders' IDs from <c>-Id</c>, or <see cref="RootFolderObject"/> instances from the pipeline,
/// as <c>Get-SonarrRootFolder</c> writes them. It collects the IDs, and once the input ends, it sends a request that
/// removes each folder, once for each ID, in ascending order of ID. It writes nothing to the pipeline.
/// </para>
/// <para>
/// Its confirm impact is High, so it confirms each folder before it removes it. <c>-Force</c> skips that prompt, but
/// not <c>-WhatIf</c> or an explicit <c>-Confirm</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Remove, "SonarrRootFolder", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true,
	DefaultParameterSetName = PSConstants.PSetExplicitId)]
[MetadataCanPipe(Tag = Meta.RootFolder)]
public sealed class RemoveSonarrRootFolderCmdlet : SonarrMetadataCmdlet
{
	private SortedSet<int> _ids = null!;

	/// <summary>
	/// Gets or sets the root folders to remove.
	/// </summary>
	/// <remarks>
	/// The parameter takes the folders from the pipeline, and each one's ID has to be greater than zero. It can't be
	/// combined with <c>-Id</c>.
	/// </remarks>
	/// <value>The folders, as <c>Get-SonarrRootFolder</c> writes them. The default is an empty array.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline)]
	[ValidateIds(ValidateRangeKind.Positive)]
	public RootFolderObject[] InputObject { get; set; } = [];

	/// <summary>
	/// Gets or sets the IDs of the root folders to remove.
	/// </summary>
	/// <remarks>
	/// Each ID has to be greater than zero. The parameter can't be combined with <c>-InputObject</c>.
	/// </remarks>
	/// <value>The IDs of the folders. The default is an empty array.</value>
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet removes the root folders without confirming each one.
	/// </summary>
	/// <remarks>
	/// The switch skips the confirmation prompt only: in What if mode the cmdlet still removes nothing, and an explicit
	/// <c>-Confirm</c> still prompts.
	/// </remarks>
	/// <value>The switch, which is present to skip the confirmation prompt.</value>
	[Parameter]
	public SwitchParameter Force { get; set; }

	/// <summary>
	/// Gets the metadata tag of the root folder endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The root folder's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.RootFolder];
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
	/// Adds the values of <c>-Id</c> to the IDs of the root folders to remove.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
	}
	/// <summary>
	/// Adds the IDs of the root folders from the pipeline to the IDs of the folders to remove.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Process(IServiceProvider provider)
	{
		if (!this.HasParameter(InputObject))
		{
			return;
		}

		foreach (RootFolderObject rootFol in this.InputObject)
		{
			if (rootFol.Id <= 0)
			{
				continue;
			}

			_ = _ids.Add(rootFol.Id);
		}
	}
	/// <summary>
	/// Removes the root folder with each of the collected IDs, in ascending order of ID.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		if (_ids.Count <= 0)
		{
			return;
		}

		bool force = this.Force.ToBool();

		foreach (int id in _ids)
		{
			this.PerformDelete(id, force);
		}
	}

	/// <summary>
	/// Removes the root folder with the specified ID once the action is confirmed.
	/// </summary>
	/// <remarks>
	/// When the request fails, the method writes its error, whatever the error is.
	/// </remarks>
	/// <param name="id">The ID of the folder to remove.</param>
	/// <param name="force"><see langword="true"/> to skip the confirmation prompt; otherwise, <see langword="false"/>.</param>
	private void PerformDelete(int id, bool force)
	{
		string url = this.Tag.GetUrlForId(id);

		if (!this.ShouldProcessWithForce(url, "Deleting Root Folder", force))
		{
			return;
		}

		var response = this.SendDeleteRequest(url);
		if (response.IsError)
		{
			this.WriteError(response.Error);
		}
	}
}

