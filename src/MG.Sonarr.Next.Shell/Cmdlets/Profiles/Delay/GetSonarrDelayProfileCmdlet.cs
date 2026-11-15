using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Delays;

/// <summary>
/// Represents the <c>Get-SonarrDelayProfile</c> cmdlet, which gets Sonarr's delay profiles.
/// </summary>
/// <remarks>
/// <para>
/// Without <c>-Id</c>, the cmdlet gets every delay profile with one request. With it, the cmdlet gets each profile with a
/// request of its own, and an ID that Sonarr doesn't have writes no error. It writes each profile to the pipeline as a
/// <see cref="DelayProfileObject"/>.
/// </para>
/// <para>
/// The cmdlet takes no pipeline input.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrDelayProfile")]
public sealed class GetSonarrDelayProfileCmdlet : SonarrMetadataCmdlet
{
	private SortedSet<int> _ids = null!;

	/// <summary>
	/// Gets or sets the IDs of the delay profiles to get.
	/// </summary>
	/// <remarks>
	/// Each ID has to be greater than zero, and an ID given more than once is requested once.
	/// </remarks>
	/// <value>The IDs of the profiles. The default is an empty array, for every profile.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = false, Position = 0)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = Array.Empty<int>();

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
	/// Gets the metadata tag of the delay profile endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The delay profile's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.DelayProfile];
	}

	/// <summary>
	/// Adds the IDs in <c>-Id</c> to the set of IDs to get.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
	}
	/// <summary>
	/// Gets the delay profiles with the IDs in <c>-Id</c>, or every delay profile when it has none, and writes them to
	/// the pipeline.
	/// </summary>
	/// <remarks>
	/// When the request for every profile fails, the cmdlet stops with its error. When the request for one ID fails, the
	/// cmdlet writes its error, unless the error is ignorable, as the one for an ID that doesn't exist is, and goes on to
	/// the next ID.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		IEnumerable<DelayProfileObject> profiles = _ids.Count > 0
			? this.GetById<DelayProfileObject>(_ids)
			: this.GetAll<DelayProfileObject>();

		this.WriteCollection(profiles);
	}
}
