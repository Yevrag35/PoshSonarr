using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.RootFolders;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.RootFolders;

/// <summary>
/// Represents the <c>Get-SonarrRootFolder</c> cmdlet, which gets the root folders that Sonarr has.
/// </summary>
/// <remarks>
/// The cmdlet writes each root folder to the pipeline as a <see cref="RootFolderObject"/>. <c>-Id</c> gets the folders
/// with those IDs, with one request for each ID, and without it, the cmdlet writes every folder.
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrRootFolder")]
public sealed class GetSonarrRootFolderCmdlet : SonarrMetadataCmdlet
{
	private SortedSet<int> _ids = null!;

	/// <summary>
	/// Gets or sets the IDs of the root folders to get.
	/// </summary>
	/// <remarks>
	/// Each ID has to be greater than zero. The cmdlet sends one request for each ID, and writes nothing for an ID that
	/// Sonarr has no folder for.
	/// </remarks>
	/// <value>The IDs of the folders. The default is an empty array, for every folder.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = false, Position = 0)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

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
	/// Gets the metadata tag of the root folder endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The root folder's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.RootFolder];
	}

	/// <summary>
	/// Adds the values of <c>-Id</c> to the cmdlet's set of IDs.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
	}

	/// <summary>
	/// Gets the root folders with the IDs, or every folder when <c>-Id</c> isn't given, and writes them to the pipeline.
	/// </summary>
	/// <remarks>
	/// A request for an ID that fails writes its error, unless the error is ignorable, as the one for an ID that doesn't
	/// exist is. When the request for every folder fails, the cmdlet stops with its error.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		IEnumerable<RootFolderObject> folders = this.HasParameter(this.Id)
			? this.GetById<RootFolderObject>(_ids)
			: this.GetAll<RootFolderObject>();

		this.WriteCollection(folders);
	}
}
