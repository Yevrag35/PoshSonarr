using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Pwsh;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.DownloadClients;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Output;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.DownloadClients;

/// <summary>
/// Represents the <c>Get-SonarrDownloadClient</c> cmdlet, which gets the download clients that Sonarr has.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet writes each client to the pipeline as a <see cref="DownloadClientObject"/>. With no <c>-Id</c> or
/// <c>-Name</c>, it writes every client.
/// </para>
/// <para>
/// <c>-Id</c> gets the clients with those IDs, with one request for each ID. <c>-Name</c> takes IDs, names, or both, and
/// the cmdlet then gets every client and keeps those that have one of the IDs or a name that matches one of the names.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrDownloadClient", DefaultParameterSetName = "ByNameOrId"), OutputType(typeof(IDownloadClientOutput))]
public sealed class GetSonarrDownloadClientCmdlet : SonarrMetadataCmdlet
{
	static readonly string _namePropertyName = nameof(Name);

	SortedSet<int> _ids = null!;
	WildcardSet _wcNames = null!;

	/// <summary>
	/// Gets or sets the IDs of the download clients to get.
	/// </summary>
	/// <remarks>
	/// The cmdlet sends one request for each ID, and writes nothing for an ID that Sonarr has no client for. The
	/// parameter can't be combined with <c>-Name</c>.
	/// </remarks>
	/// <value>The IDs of the clients. The default is an empty array.</value>
	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSetExplicitId)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets the IDs and names of the download clients to get.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Given by position, a number, or text that reads as a whole number, is a client's ID, and any other text is a
	/// name. Given as <c>-Name</c>, every value is a name, a number included.
	/// </para>
	/// <para>
	/// A name can hold wildcards and matches a client's name ignoring case. The cmdlet gets every client and keeps those
	/// that have one of the IDs or a name that matches one of the names.
	/// </para>
	/// </remarks>
	/// <value>The IDs and names of the clients. The default is an empty array, for every client.</value>
	[Parameter(Position = 0, ParameterSetName = "ByNameOrId")]
	public Either<string, int>[] Name { get; set; } = [];

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
	/// Gets the metadata tag of the download client endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The download client's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.DownloadClient];
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
	/// Gets the download clients and writes them to the pipeline.
	/// </summary>
	/// <remarks>
	/// With IDs and no names, the cmdlet gets each client by its ID, and writes a failed request's error unless it's
	/// ignorable, as the one for an ID that doesn't exist is. Otherwise it gets every client, and when that request
	/// fails, the cmdlet stops with its error.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		IList<DownloadClientObject> dlObjs = _ids.Count > 0 && _wcNames.Count == 0
			? this.GetById<DownloadClientObject>(_ids)
			: this.GetByName(_wcNames, _ids);

		this.WriteCollection(dlObjs);
	}

	/// <summary>
	/// Gets every download client and removes from the list those that have none of the specified IDs and a name that
	/// matches none of the specified names.
	/// </summary>
	/// <remarks>
	/// When both sets are empty, the list keeps every client. When the request fails, the cmdlet stops with its error,
	/// and the list is empty.
	/// </remarks>
	/// <param name="names">The names, which can hold wildcards, that the clients' names are matched against.</param>
	/// <param name="ids">The IDs of the clients to keep.</param>
	/// <returns>The clients that match, or every client when there's nothing to match.</returns>
	private MetadataList<DownloadClientObject> GetByName(WildcardSet names, SortedSet<int> ids)
	{
		var all = this.GetAll<DownloadClientObject>();

		if (all.Count > 0 && (names.Count > 0 || ids.Count > 0))
		{
			FnPtr<DownloadClientObject, WildcardSet, SortedSet<int>, bool> removePtr;
			unsafe { removePtr = new(&removeIf); }

			all.RemoveAll(names, ids, removePtr);
		}

		return all;

		static bool removeIf(DownloadClientObject item, WildcardSet names, SortedSet<int> ids)
		{
			return !ids.Contains(item.Id) && !names.IsAnyMatch(item.Name);
		}
	}
}
