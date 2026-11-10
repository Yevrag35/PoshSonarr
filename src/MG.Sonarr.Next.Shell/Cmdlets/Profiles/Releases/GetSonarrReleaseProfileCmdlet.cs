using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Pwsh;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Releases;

/// <summary>
/// Represents the <c>Get-SonarrReleaseProfile</c> cmdlet, which gets Sonarr's release profiles.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet gets the profiles with the IDs that it's given, each with a request of its own, and the profiles whose
/// names match <c>-Name</c>, from one request for every profile. With neither, it gets every profile. It writes
/// the profiles to the pipeline as <see cref="ReleaseProfileObject"/> instances.
/// </para>
/// <para>
/// <c>-Name</c>, at position 0, takes names and IDs, and <c>-Id</c> takes IDs only. The cmdlet takes no pipeline input.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrReleaseProfile")]
public sealed class GetSonarrReleaseProfileCmdlet : SonarrMetadataCmdlet
{
	private SortedSet<int> _ids = null!;
	private WildcardSet _wcNames = null!;

	/// <summary>
	/// Gets or sets the IDs of the release profiles to get.
	/// </summary>
	/// <remarks>
	/// Each ID has to be greater than zero, and an ID given more than once is requested once. An ID that Sonarr doesn't
	/// have writes no error. The parameter can't be combined with <c>-Name</c>.
	/// </remarks>
	/// <value>The IDs of the profiles.</value>
	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets the names or IDs of the release profiles to get.
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
	[Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByProfileNameOrId")]
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
		_wcNames = [];
	}

	/// <summary>
	/// Gets the metadata tag of the release profile endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The release profile's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.ReleaseProfile];
	}

	/// <summary>
	/// Adds the IDs in <c>-Id</c> to the set of IDs to get, and splits <c>-Name</c> into the sets of IDs and names.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
		if (this.HasParameter(this.Name))
		{
			this.Name.SplitToSets(_ids, _wcNames, !this.MyInvocation.IsBoundPositionally(nameof(this.Name)));
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
	protected override void Process(IServiceProvider provider)
	{
		bool hasIds = _ids.Count > 0;

		if (hasIds)
		{
			this.WriteCollection(this.GetById<ReleaseProfileObject>(_ids));
		}

		if (!hasIds || !_wcNames.IsNullOrEmpty)
		{
			this.WriteCollection(this.GetByName(_wcNames));
		}
	}

	/// <summary>
	/// Gets every release profile and keeps those whose names match one of the specified names.
	/// </summary>
	/// <remarks>
	/// The method reads a profile's name from its <c>name</c> property, and a profile without a name, or whose name is
	/// <see langword="null"/>, is left out. When the request fails, the cmdlet stops with its error, and the list is
	/// empty.
	/// </remarks>
	/// <param name="names">
	/// The names, which can hold wildcards, that the profiles' names are matched against, or an empty set to keep every
	/// profile.
	/// </param>
	/// <returns>The profiles that match, or every profile when <paramref name="names"/> is empty.</returns>
	private MetadataList<ReleaseProfileObject> GetByName(WildcardSet names)
	{
		var response = this.SendGetRequest<MetadataList<ReleaseProfileObject>>(this.Tag.UrlBase);
		if (response.IsError)
		{
			this.StopCmdlet(response.Error);
			return [];
		}
		else if (names.IsNullOrEmpty)
		{
			return response.Value;
		}

		for (int i = response.Value.Count - 1; i >= 0; i--)
		{
			var profile = response.Value[i];
			if (!profile.TryGetNonNullProperty(Constants.Name, out string? name)
				||
				!names.IsAnyMatch(name))
			{
				response.Value.RemoveAt(i);
			}
		}

		return response.Value;
	}
}
