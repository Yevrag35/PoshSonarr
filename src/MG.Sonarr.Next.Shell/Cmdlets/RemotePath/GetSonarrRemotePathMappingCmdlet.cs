using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.RemotePaths;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.RemotePath;

/// <summary>
/// Represents the <c>Get-SonarrRemotePathMapping</c> cmdlet, which gets the remote path mappings that Sonarr has.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet writes each mapping to the pipeline as a <see cref="RemotePathObject"/>. <c>-Id</c> gets the mappings
/// with those IDs, with one request for each ID, and without it, the cmdlet writes every mapping.
/// </para>
/// <para>
/// The cmdlet's aliases are <c>Get-SonarrRemotePath</c> and <c>Get-SonarrRemoteMapping</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrRemotePathMapping")]
[Alias("Get-SonarrRemotePath", "Get-SonarrRemoteMapping")]
public sealed class GetSonarrRemotePathMappingCmdlet : SonarrMetadataCmdlet
{
	SortedSet<int> _ids = null!;

	/// <summary>
	/// Gets or sets the IDs of the remote path mappings to get.
	/// </summary>
	/// <remarks>
	/// Each ID has to be greater than zero. The cmdlet sends one request for each ID, and writes nothing for an ID that
	/// Sonarr has no mapping for.
	/// </remarks>
	/// <value>The IDs of the mappings. The default is an empty array, for every mapping.</value>
	[Parameter(Mandatory = false, Position = 0)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = Array.Empty<int>();

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
	/// Adds the values of <c>-Id</c> to the cmdlet's set of IDs.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
	}
	/// <summary>
	/// Gets the remote path mappings with the IDs, or every mapping when there are none, and writes them to the
	/// pipeline.
	/// </summary>
	/// <remarks>
	/// A request for an ID that fails writes its error, unless the error is ignorable, as the one for an ID that doesn't
	/// exist is. When the request for every mapping fails, the cmdlet stops with its error.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		IEnumerable<RemotePathObject> remotePaths = _ids.Count > 0
			? this.GetById<RemotePathObject>(_ids)
			: this.GetAll<RemotePathObject>();

		this.WriteCollection(remotePaths);
	}
}

