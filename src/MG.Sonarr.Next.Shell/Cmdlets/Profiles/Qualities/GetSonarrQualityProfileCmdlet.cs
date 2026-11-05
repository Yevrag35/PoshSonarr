using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Pwsh;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Qualities;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Qualities;

/// <summary>
/// Represents the <c>Get-SonarrQualityProfile</c> cmdlet, which gets Sonarr's quality profiles.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet gets the profiles with the IDs that it's given, each with a request of its own, and the profiles whose
/// names match <c>-Name</c>, from one request for every profile. With neither, it gets every profile. It writes the
/// profiles to the pipeline as <see cref="QualityProfileObject"/> instances, those found by ID first, and a profile
/// that both an ID and a name pick is written once.
/// </para>
/// <para>
/// <c>-Name</c>, at position 0, takes names and IDs, <c>-Id</c> takes IDs only, and a series piped to the cmdlet, such
/// as one that <c>Get-SonarrSeries</c> writes, gets the series' quality profile. The cmdlet collects the IDs from every
/// input and sends its requests when the input ends.
/// </para>
/// <para>
/// The cmdlet's alias is <c>Get-SonarrProfile</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrQualityProfile", DefaultParameterSetName = "ByProfileName")]
[Alias("Get-SonarrProfile")]
[MetadataCanPipe(Tag = Meta.Series)]
public sealed class GetSonarrQualityProfileCmdlet : SonarrMetadataCmdlet
{
	static readonly string _namePropertyName = nameof(Name);
	SortedSet<int> _ids = null!;
	WildcardSet _wcNames = null!;

	/// <summary>
	/// Gets or sets the objects whose quality profiles the cmdlet gets, from the pipeline.
	/// </summary>
	/// <remarks>
	/// The parameter takes the objects that carry a quality profile's ID, such as series, and each ID has to be greater
	/// than zero.
	/// </remarks>
	/// <value>The objects whose quality profiles' IDs the cmdlet gets.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSetPipeline, ValueFromPipeline = true)]
	[ValidateIds<IQualityProfilePipeable>(ValidateRangeKind.Positive)]
	public IQualityProfilePipeable[] InputObject { get; set; } = [];

	/// <summary>
	/// Gets or sets the IDs of the quality profiles to get.
	/// </summary>
	/// <remarks>
	/// Each ID has to be greater than zero, and an ID given more than once is requested once. An ID that Sonarr doesn't
	/// have writes no error. The parameter can't be combined with <c>-Name</c> or pipeline input.
	/// </remarks>
	/// <value>The IDs of the profiles.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets the names or IDs of the quality profiles to get.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Given by position, a number, or text that reads as a whole number, is a profile's ID, and any other text is a
	/// name. Given by name, as <c>-Name</c>, every value is a name, a number included.
	/// </para>
	/// <para>
	/// A name can hold wildcards, and it matches a profile's name ignoring case.
	/// </para>
	/// </remarks>
	/// <value>The names and IDs of the profiles. The default is an empty array, for every profile.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByProfileName")]
	[SupportsWildcards]
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
	/// Gets the metadata tag of the quality profile endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The quality profile's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.QualityProfile];
	}

	/// <summary>
	/// Adds the IDs in <c>-Id</c> to the set of IDs to get, and splits <c>-Name</c> into the sets of IDs and names.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
		if (this.Name.Length > 0)
		{
			this.Name.SplitToSets(_ids, _wcNames, !this.MyInvocation.IsBoundPositionally(_namePropertyName));
		}
	}
	/// <summary>
	/// Adds the quality profile IDs of the piped objects to the set of IDs to get.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		if (this.InputObject.Length > 0)
		{
			_ids.UnionWith(
				this.InputObject.Where(x => x.QualityProfileId > 0).Select(x => x.QualityProfileId));
		}
	}
	/// <summary>
	/// Gets the profiles with the collected IDs and those whose names match the collected names, or every profile when
	/// there are neither, and writes them to the pipeline.
	/// </summary>
	/// <remarks>
	/// When the request for one ID fails, the cmdlet writes its error, unless the error is ignorable, and goes on to the
	/// next ID. When the request for every profile fails, the cmdlet stops with its error.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		List<QualityProfileObject> list = new(_ids.Count + _wcNames.Count);

		bool addedIds = false;
		if (_ids.Count > 0)
		{
			IEnumerable<QualityProfileObject> profiles = this.GetById<QualityProfileObject>(_ids);
			list.AddRange(profiles);
			addedIds = true;
		}

		if (_wcNames.Count > 0 || !addedIds)
		{
			var all = this.GetAll<QualityProfileObject>();
			if (all.Count > 0)
			{
				FilterByProfileName(all, _wcNames, _ids);
			}

			list.AddRange(all);
		}

		this.WriteCollection(list);
	}
	/// <summary>
	/// Removes from the specified list the profiles whose IDs are in the specified set or whose names match none of the
	/// specified names.
	/// </summary>
	/// <remarks>
	/// When both sets are empty, the method leaves the list as it is, so it keeps every profile.
	/// </remarks>
	/// <param name="list">Every profile, which the method removes profiles from.</param>
	/// <param name="names">The names, which can hold wildcards, that the profiles' names are matched against.</param>
	/// <param name="ids">The IDs of the profiles that the cmdlet already got by ID, which the list leaves out.</param>
	private static void FilterByProfileName(MetadataList<QualityProfileObject> list, WildcardSet names, SortedSet<int> ids)
	{
		if (ids.Count == 0 && names.Count == 0)
		{
			return;
		}

		for (int i = list.Count - 1; i >= 0; i--)
		{
			QualityProfileObject item = list[i];
			if (ids.Contains(item.Id) || !names.IsAnyMatch(item.Name))
			{
				list.RemoveAt(i);
			}
		}
	}
}
