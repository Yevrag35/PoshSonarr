using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Languages;

/// <summary>
/// Represents the <c>Get-SonarrLanguageProfile</c> cmdlet, which gets Sonarr's language profiles.
/// </summary>
/// <remarks>
/// <para>
/// Sonarr has deprecated its language profile endpoints, and so the cmdlet is obsolete.
/// </para>
/// <para>
/// The cmdlet gets the profiles with the IDs in <c>-Id</c>, each with a request of its own, and the profiles whose names
/// match <c>-Name</c>, from one request for every profile. With neither, it gets every profile. It writes each profile to
/// the pipeline as a <see cref="LanguageProfileObject"/>, and a profile that both an ID and a name pick is written once.
/// </para>
/// <para>
/// A series piped to the cmdlet, such as one that <c>Get-SonarrSeries</c> writes, gets the series' language profile.
/// The cmdlet collects the IDs from every input and sends its requests when the input ends.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrLanguageProfile", DefaultParameterSetName = "None")]
[MetadataCanPipe(Tag = Meta.Series)]
[Obsolete("The LanguageProfileController and its endpoints are deprecated.")]
public sealed class GetSonarrLanguageProfileCmdlet : SonarrMetadataCmdlet
{
	private SortedSet<int> _ids = null!;
	private WildcardSet _wcNames = null!;

	/// <summary>
	/// Gets or sets the IDs of the language profiles to get.
	/// </summary>
	/// <remarks>
	/// Each ID has to be greater than zero, and an ID given more than once is requested once. An ID that Sonarr doesn't
	/// have writes no error.
	/// </remarks>
	/// <value>The IDs of the profiles.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSetExplicitId, Position = 0)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets the objects whose language profiles the cmdlet gets, from the pipeline.
	/// </summary>
	/// <remarks>
	/// The parameter takes the objects that carry a language profile's ID, such as series, and each ID has to be greater
	/// than zero. It's hidden from tab completion and the cmdlet's syntax, since it's meant for pipeline input.
	/// </remarks>
	/// <value>The objects whose language profiles' IDs the cmdlet gets.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline, DontShow = true)]
	[ValidateIds<ILanguageProfilePipeable>(ValidateRangeKind.Positive)]
	public ILanguageProfilePipeable[] InputObject { get; set; } = [];

	/// <summary>
	/// Gets or sets the names of the language profiles to get.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A name can hold wildcards, and it matches a profile's name ignoring case.
	/// </para>
	/// <para>
	/// The parameter comes first by position, except after <c>-Id</c>, where it comes second, so
	/// <c>Get-SonarrLanguageProfile 1 Eng*</c> gives both.
	/// </para>
	/// </remarks>
	/// <value>The names of the profiles. The default is an empty array.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = false, Position = 0, ParameterSetName = "None")]
	[Parameter(Mandatory = false, Position = 1, ParameterSetName = PSConstants.PSetExplicitId)]
	[Parameter(Mandatory = false, Position = 0, ParameterSetName = PSConstants.PSetPipeline)]
	[SupportsWildcards]
	public string[] Name { get; set; } = [];

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
	/// Gets the metadata tag of the language profile endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The language profile's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.LanguageProfile];
	}

	/// <summary>
	/// Adds the IDs in <c>-Id</c> and the names in <c>-Name</c> to the sets of IDs and names to get.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
		if (this.HasParameter(this.Name))
		{
			_wcNames.UnionWith(this.Name);
		}
	}
	/// <summary>
	/// Adds the language profile IDs of the piped objects to the set of IDs to get.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Process(IServiceProvider provider)
	{
		if (this.InputObject.Length > 0)
		{
			_ids.AddRange(this.InputObject);
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
		bool addedIds = false;
		if (_ids.Count > 0)
		{
			addedIds = true;
			var fromIds = this.GetById<LanguageProfileObject>(_ids);
			this.WriteCollection(fromIds);
		}

		if (_wcNames.Count > 0 || !addedIds)
		{
			var fromNames = this.GetByName(_wcNames, _ids);
			this.WriteCollection(fromNames);
		}
	}

	/// <summary>
	/// Gets every language profile and keeps those whose names match one of the specified names and whose IDs aren't
	/// in the specified set.
	/// </summary>
	/// <remarks>
	/// When the request fails, the cmdlet stops with its error, and the list is empty.
	/// </remarks>
	/// <param name="names">The names, which can hold wildcards, that the profiles' names are matched against, or an
	/// empty set to keep every profile.</param>
	/// <param name="ids">The IDs of the profiles that the cmdlet already got by ID, which the list leaves out.</param>
	/// <returns>
	/// The profiles that match, or every profile when <paramref name="names"/> is empty.
	/// </returns>
	private MetadataList<LanguageProfileObject> GetByName(WildcardSet names, SortedSet<int> ids)
	{
		MetadataList<LanguageProfileObject> response = this.GetAll<LanguageProfileObject>();
		if (response.Count == 0 || names.Count == 0)
		{
			return response;
		}

		for (int i = response.Count - 1; i >= 0; i--)
		{
			var item = response[i];
			if (ids.Contains(item.Id) || !names.IsAnyMatch(item.Name))
			{
				response.RemoveAt(i);
			}
		}

		return response;
	}
}
