using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Indexers;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.Indexers;

/// <summary>
/// Represents the <c>Remove-SonarrIndexer</c> cmdlet, which deletes indexers from Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes the indexers' IDs from <c>-Id</c>, or <see cref="IndexerObject"/> instances from the pipeline, as
/// <c>Get-SonarrIndexer</c> writes them. It collects the IDs, and once the input ends, it sends a request that deletes
/// each indexer, once for each ID, in ascending order of ID. It writes nothing to the pipeline, and writes a verbose
/// message for each indexer that it deletes.
/// </para>
/// <para>
/// Its confirm impact is High, so it confirms each indexer before it deletes it. <c>-Force</c> skips that prompt, but
/// not <c>-WhatIf</c> or an explicit <c>-Confirm</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Remove, "SonarrIndexer", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true,
	DefaultParameterSetName = PSConstants.PSetExplicitId)]
[MetadataCanPipe(Tag = Meta.Indexer)]
public sealed class RemoveSonarrIndexerCmdlet : SonarrMetadataCmdlet
{
	SortedSet<int> _ids = null!;

	/// <summary>
	/// Gets or sets the IDs of the indexers to delete.
	/// </summary>
	/// <remarks>
	/// Each ID has to be greater than zero. The parameter can't be combined with <c>-InputObject</c>.
	/// </remarks>
	/// <value>The IDs of the indexers. The default is an empty array.</value>
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets the indexers to delete.
	/// </summary>
	/// <remarks>
	/// The parameter takes the indexers from the pipeline, and each one's ID has to be greater than zero. It can't be
	/// combined with <c>-Id</c>.
	/// </remarks>
	/// <value>The indexers, as <c>Get-SonarrIndexer</c> writes them. The default is an empty array.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline)]
	[ValidateIds(ValidateRangeKind.Positive)]
	public IndexerObject[] InputObject { get; set; } = [];

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet deletes the indexers without confirming each one.
	/// </summary>
	/// <remarks>
	/// The switch skips the confirmation prompt only: in What if mode the cmdlet still deletes nothing, and an explicit
	/// <c>-Confirm</c> still prompts.
	/// </remarks>
	/// <value>The switch, which is present to skip the confirmation prompt.</value>
	[Parameter]
	public SwitchParameter Force { get; set; }

	/// <summary>
	/// Gets the metadata tag of the indexer endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The indexer's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.Indexer];
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
	/// Adds the values of <c>-Id</c> to the IDs of the indexers to delete.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
	}

	/// <summary>
	/// Adds the IDs of the indexers from the pipeline to the IDs of the indexers to delete.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		if (this.InputObject.Length > 0)
		{
			_ids.UnionWith(this.InputObject.Select(x => x.Id));
		}
	}

	/// <summary>
	/// Deletes the indexer with each of the collected IDs, in ascending order of ID.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		if (_ids.Count <= 0)
		{
			return;
		}

		foreach (int id in _ids)
		{
			this.DeleteIndexer(id);
		}
	}

	/// <summary>
	/// Deletes the indexer with the specified ID once the action is confirmed, and writes a verbose message when it's
	/// deleted.
	/// </summary>
	/// <remarks>
	/// <c>-Force</c> skips the confirmation prompt. When the request fails, the method writes its error, whatever the
	/// error is.
	/// </remarks>
	/// <param name="id">The ID of the indexer to delete.</param>
	private void DeleteIndexer(int id)
	{
		string url = this.Tag.GetUrlForId(id);
		if (!this.ShouldProcessWithForce(url, "Deleting Indexer", this.Force))
		{
			return;
		}

		var response = this.SendDeleteRequest(url);
		if (response.IsError)
		{
			this.WriteError(response.Error);
			return;
		}

		this.WriteVerbose($"Deleted Indexer -> {id}");
	}
}

