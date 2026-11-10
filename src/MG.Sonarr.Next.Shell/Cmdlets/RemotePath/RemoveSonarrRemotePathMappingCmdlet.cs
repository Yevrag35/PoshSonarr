using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.RemotePaths;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.RemotePath;

/// <summary>
/// Represents the <c>Remove-SonarrRemotePathMapping</c> cmdlet, which deletes remote path mappings from Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes the mappings' IDs from <c>-Id</c>, or <see cref="RemotePathObject"/> instances from the pipeline,
/// as <c>Get-SonarrRemotePathMapping</c> writes them. It collects the IDs, and once the input ends, it sends a request
/// that deletes each mapping, once for each ID, in ascending order of ID. It writes nothing to the pipeline, and writes
/// a verbose message for each mapping that it deletes.
/// </para>
/// <para>
/// Its confirm impact is High, so it confirms each mapping before it deletes it. <c>-Force</c> skips that prompt, but
/// not <c>-WhatIf</c> or an explicit <c>-Confirm</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Remove, "SonarrRemotePathMapping", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High, DefaultParameterSetName = PSConstants.PSetExplicitId)]
[MetadataCanPipe(Tag = Meta.RemotePathMapping)]
public sealed class RemoveSonarrRemotePathMappingCmdlet : SonarrMetadataCmdlet
{
	private SortedSet<int> _ids = null!;

	/// <summary>
	/// Gets or sets the IDs of the remote path mappings to delete.
	/// </summary>
	/// <remarks>
	/// Each ID has to be greater than zero. The parameter can't be combined with <c>-InputObject</c>.
	/// </remarks>
	/// <value>The IDs of the mappings. The default is an empty array.</value>
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = Array.Empty<int>();

	/// <summary>
	/// Gets or sets the remote path mappings to delete.
	/// </summary>
	/// <remarks>
	/// The parameter takes the mappings from the pipeline, and each one's ID has to be greater than zero. It can't be
	/// combined with <c>-Id</c>.
	/// </remarks>
	/// <value>The mappings, as <c>Get-SonarrRemotePathMapping</c> writes them. The default is an empty array.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline)]
	[ValidateIds(ValidateRangeKind.Positive)]
	public RemotePathObject[] InputObject { get; set; } = Array.Empty<RemotePathObject>();

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet deletes the remote path mappings without confirming each
	/// one.
	/// </summary>
	/// <remarks>
	/// The switch skips the confirmation prompt only: in What if mode the cmdlet still deletes nothing, and an explicit
	/// <c>-Confirm</c> still prompts.
	/// </remarks>
	/// <value>The switch, which is present to skip the confirmation prompt.</value>
	[Parameter]
	public SwitchParameter Force { get; set; }

	/// <summary>
	/// Gets the metadata tag of the remote path mapping endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The remote path mapping's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.RemotePathMapping];
	}
	/// <summary>
	/// Creates the set of IDs that the cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		_ids = [];
	}

	/// <summary>
	/// Adds the values of <c>-Id</c> to the IDs of the remote path mappings to delete.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
	}
	/// <summary>
	/// Adds the IDs of the remote path mappings from the pipeline to the IDs of the mappings to delete.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		if (PSConstants.PSetPipeline == this.ParameterSetName)
		{
			_ids.UnionWith(this.InputObject.Select(x => x.Id));
		}
	}
	/// <summary>
	/// Deletes the remote path mapping with each of the collected IDs, in ascending order of ID.
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
			this.DeleteRemotePathMapping(in id, this.Tag, in force);
		}
	}

	/// <summary>
	/// Deletes the remote path mapping with the specified ID once the action is confirmed, and writes a verbose message
	/// when it's deleted.
	/// </summary>
	/// <remarks>
	/// When the request fails, the method writes its error, whatever the error is.
	/// </remarks>
	/// <param name="id">The ID of the mapping to delete.</param>
	/// <param name="tag">The metadata tag of the remote path mapping endpoint, which builds the mapping's URL.</param>
	/// <param name="force"><see langword="true"/> to skip the confirmation prompt; otherwise, <see langword="false"/>.</param>
	private void DeleteRemotePathMapping(in int id, MetadataTag tag, in bool force)
	{
		string url = tag.GetUrlForId(id);
		if (!this.ShouldProcessWithForce(url, "Deleting Remote Path Mapping", force))
		{
			return;
		}

		var response = this.SendDeleteRequest(url);
		if (response.IsError)
		{
			this.WriteError(response.Error);
			return;
		}

		this.WriteVerbose($"Deleted Remote Path Mapping -> {id}");
	}
}

