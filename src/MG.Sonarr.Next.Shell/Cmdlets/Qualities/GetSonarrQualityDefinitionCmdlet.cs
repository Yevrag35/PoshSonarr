using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Pwsh;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Qualities;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Qualities;

/// <summary>
/// Represents the <c>Get-SonarrQualityDefinition</c> cmdlet, which gets Sonarr's quality definitions, the size limits
/// of each quality.
/// </summary>
/// <remarks>
/// <para>
/// Without arguments, the cmdlet gets every definition. <c>-Id</c> gets the definitions with the given IDs, and
/// <c>-Name</c> the definitions with the given IDs or with titles that match the given names. The cmdlet writes each
/// definition to the pipeline as a <see cref="QualityDefinitionObject"/>: those found by ID first, then those whose
/// titles match.
/// </para>
/// <para>
/// Each ID is got with a request of its own, and an ID that Sonarr doesn't have writes no error. Names are matched
/// against every definition, got with one request, and when that request fails, the cmdlet stops with its error.
/// </para>
/// <para>
/// The cmdlet's alias is <c>Get-SonarrQuality</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrQualityDefinition")]
[Alias("Get-SonarrQuality")]
public sealed class GetSonarrQualityDefinitionCmdlet : SonarrMetadataCmdlet
{
	static readonly string _namePropertyName = nameof(Name);
	SortedSet<int> _ids = null!;
	WildcardSet _wcNames = null!;
	List<QualityDefinitionObject> _list = null!;

	/// <summary>
	/// Gets or sets the IDs of the quality definitions to get.
	/// </summary>
	/// <remarks>
	/// Each ID has to be greater than zero.
	/// </remarks>
	/// <value>The definition IDs. The default is an empty array.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = false)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets the IDs and titles of the quality definitions to get.
	/// </summary>
	/// <remarks>
	/// Given by position, a number, or text that reads as a whole number, is a definition's ID, and any other text is a
	/// title, which can hold wildcards and matches ignoring case. Given by name, as <c>-Name</c>, every value is a title,
	/// a number as its text.
	/// </remarks>
	/// <value>The IDs and titles of the definitions. The default is an empty array, for every definition.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = false, Position = 0)]
	[SupportsWildcards]
	public Either<string, int>[] Name { get; set; } = [];

	/// <summary>
	/// Runs the base class's setup for the scope, then creates the sets of IDs and titles that the cmdlet fills and the
	/// list of definitions that it writes.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = [];
		_wcNames = new();
		_list = new(1);
	}
	/// <summary>
	/// Gets the metadata tag of the quality definition endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The quality definition endpoint's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.QualityDefinition];
	}

	/// <summary>
	/// Collects the IDs from <c>-Id</c>, and the IDs and titles from <c>-Name</c>.
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
	/// Gets the definitions with the collected IDs, then, when titles were given or no ID was, every definition, and
	/// keeps those whose titles match and that weren't already got by ID.
	/// </summary>
	/// <remarks>
	/// When neither IDs nor titles were given, every definition is kept.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		bool addedIds = false;
		if (_ids.Count > 0)
		{
			_list.AddRange(this.GetById<QualityDefinitionObject>(_ids));
			addedIds = true;
		}

		if (_wcNames.Count > 0 || !addedIds)
		{
			var all = this.GetAll<QualityDefinitionObject>();
			if (all.Count > 0)
			{
				FilterByName(all, _ids, _wcNames);
				_list.AddRange(all);
			}
		}
	}
	/// <summary>
	/// Writes the definitions that the cmdlet got to the pipeline.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		this.WriteCollection(_list);
	}

	/// <summary>
	/// Removes from the specified list each definition whose ID is in the specified set or whose title matches none of
	/// the specified names, and leaves the list alone when there are no names.
	/// </summary>
	/// <param name="all">Every definition, which the method removes definitions from.</param>
	/// <param name="ids">The IDs of the definitions that were already got by ID.</param>
	/// <param name="names">The names, which can hold wildcards, that the titles are matched against.</param>
	private static void FilterByName(MetadataList<QualityDefinitionObject> all, SortedSet<int> ids, WildcardSet names)
	{
		if (names.Count == 0)
		{
			return;
		}

		for (int i = all.Count - 1; i >= 0; i--)
		{
			QualityDefinitionObject item = all[i];
			if (ids.Contains(item.Id) || !names.IsAnyMatch(item.Title))
			{
				all.RemoveAt(i);
			}
		}
	}
}
