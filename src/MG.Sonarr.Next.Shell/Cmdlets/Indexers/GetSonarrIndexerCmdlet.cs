using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Pwsh;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Indexers;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Indexers;

/// <summary>
/// Represents the <c>Get-SonarrIndexer</c> cmdlet, which gets the indexers that Sonarr has.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet writes each indexer to the pipeline as an <see cref="IndexerObject"/>. With no <c>-Id</c> or
/// <c>-Name</c>, it writes every indexer.
/// </para>
/// <para>
/// <c>-Id</c> gets the indexers with those IDs, with one request for each ID. <c>-Name</c> takes IDs, names, or both,
/// and the cmdlet then gets every indexer and keeps those that have one of the IDs or a name that matches one of the
/// names.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrIndexer")]
public sealed class GetSonarrIndexerCmdlet : SonarrMetadataCmdlet
{
	static readonly string _namePropertyName = nameof(Name);

	SortedSet<int> _ids = null!;
	WildcardSet _wcNames = null!;

	/// <summary>
	/// Gets or sets the IDs of the indexers to get.
	/// </summary>
	/// <remarks>
	/// The cmdlet sends one request for each ID, and writes nothing for an ID that Sonarr has no indexer for. The
	/// parameter can't be combined with <c>-Name</c>.
	/// </remarks>
	/// <value>The IDs of the indexers. The default is an empty array.</value>
	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSetExplicitId)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets the IDs and names of the indexers to get.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Given by position, a number, or text that reads as a whole number, is an indexer's ID, and any other text is a
	/// name. Given as <c>-Name</c>, every value is a name, a number included.
	/// </para>
	/// <para>
	/// A name can hold wildcards and matches an indexer's name ignoring case. The cmdlet gets every indexer and keeps
	/// those that have one of the IDs or a name that matches one of the names.
	/// </para>
	/// </remarks>
	/// <value>The IDs and names of the indexers. The default is an empty array, for every indexer.</value>
	[Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByIndexerNameOrId")]
	public Either<string, int>[] Name { get; set; } = [];

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
	/// Runs the base class's setup for the scope, then creates the sets of IDs and names that the cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = [];
		_wcNames = new();
	}

	/// <summary>
	/// Adds the values of <c>-Id</c> to the cmdlet's IDs, and splits the values of <c>-Name</c> into IDs and names.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
		if (this.HasParameter(this.Name))
		{
			this.Name.SplitToSets(_ids, _wcNames, !this.MyInvocation.IsBoundPositionally(_namePropertyName));
		}
	}

	/// <summary>
	/// Gets the indexers and writes them to the pipeline.
	/// </summary>
	/// <remarks>
	/// With IDs and no names, the cmdlet gets each indexer by its ID, and writes a failed request's error unless it's
	/// ignorable, as the one for an ID that doesn't exist is. Otherwise it gets every indexer, and when that request
	/// fails, the cmdlet stops with its error.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		IList<IndexerObject> indexers = _ids.Count > 0 && _wcNames.Count == 0
			? this.GetById<IndexerObject>(_ids)
			: this.GetByName(_wcNames, _ids);

		this.WriteCollection(indexers);
	}

	/// <summary>
	/// Gets every indexer and, when there are names, removes from the list those that have none of the specified IDs
	/// and a name that matches none of the names.
	/// </summary>
	/// <remarks>
	/// With no names, the list keeps every indexer. When the request fails, the cmdlet stops with its error, and the
	/// list is empty.
	/// </remarks>
	/// <param name="names">The names, which can hold wildcards, that the indexers' names are matched against.</param>
	/// <param name="ids">The IDs of the indexers to keep.</param>
	/// <returns>The indexers that match, or every indexer when there are no names.</returns>
	private MetadataList<IndexerObject> GetByName(WildcardSet names, SortedSet<int> ids)
	{
		var response = this.GetAll<IndexerObject>();
		if (response.Count == 0 || names.Count == 0)
		{
			return response;
		}

		for (int i = response.Count - 1; i >= 0; i--)
		{
			var item = response[i];
			if (!ids.Contains(item.Id)
				&&
				!names.IsAnyMatch(item.Name))
			{
				response.RemoveAt(i);
			}
		}

		return response;
	}
}
