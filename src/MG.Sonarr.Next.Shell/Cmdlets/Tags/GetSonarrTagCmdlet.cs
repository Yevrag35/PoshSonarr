using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Pwsh;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags;

/// <summary>
/// Represents the <c>Get-SonarrTag</c> cmdlet, which gets tags from Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet writes each tag to the pipeline as a <see cref="TagObject"/>. Without arguments, it gets every tag.
/// <c>-Name</c> gets the tags with the given IDs or labels, and <c>-Id</c> the tags with the given IDs. Piped
/// objects that carry tags, such as series, delay profiles, download clients, indexers, and release profiles, taken
/// through <c>-InputObject</c> as <see cref="ITagPipeable"/> values, get the tags that they carry.
/// </para>
/// <para>
/// When only IDs are given, the cmdlet gets each tag with a request of its own, and an ID that Sonarr doesn't have
/// writes no error. Otherwise it gets every tag with one request and keeps those that match, and when that request
/// fails, the cmdlet stops with its error. The tags are written when the pipeline ends.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrTag", DefaultParameterSetName = PSConstants.PSetByName)]
[MetadataCanPipe(Tag = Meta.DelayProfile)]
[MetadataCanPipe(Tag = Meta.DownloadClient)]
[MetadataCanPipe(Tag = Meta.Indexer)]
[MetadataCanPipe(Tag = Meta.ReleaseProfile)]
[MetadataCanPipe(Tag = Meta.Series)]
[MetadataCanPipe(Tag = Meta.SeriesAdd)]
public sealed class GetSonarrTagCmdlet : SonarrMetadataCmdlet
{
	SortedSet<int> _ids = null!;
	WildcardSet _wcNames = null!;

	/// <summary>
	/// Gets or sets the objects whose tags the cmdlet gets.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input. Each object has to implement <see cref="ITagPipeable"/> and have an ID
	/// greater than zero. An object without tags adds none.
	/// </remarks>
	/// <value>The objects that carry tags. The default is an empty array.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline)]
	[ValidateIds<ITagPipeable>(ValidateRangeKind.Positive)]
	public ITagPipeable[] InputObject { get; set; } = [];

	/// <summary>
	/// Gets or sets the IDs of the tags to get.
	/// </summary>
	/// <remarks>
	/// Each ID has to be greater than zero.
	/// </remarks>
	/// <value>The tag IDs. The default is an empty array.</value>
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets the IDs and labels of the tags to get.
	/// </summary>
	/// <remarks>
	/// Given by position, a number, or text that reads as a whole number, is a tag's ID, which has to be greater than
	/// zero, and any other text is a label, which can hold wildcards and matches ignoring case. Given by name, as
	/// <c>-Name</c>, every value is a label, a number as its text.
	/// </remarks>
	/// <value>The IDs and labels of the tags. The default is an empty array, for every tag.</value>
	[Parameter(Mandatory = false, Position = 0, ParameterSetName = PSConstants.PSetByName)]
	[SupportsWildcards]
	[ValidateIds(ValidateRangeKind.Positive, NullBehavior = InputNullBehavior.Ignore)]
	public Either<string, int>[] Name { get; set; } = [];

	/// <summary>
	/// Runs the base class's setup for the scope, then creates the sets of tag IDs and labels that the cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = [];
		_wcNames = new();
	}
	/// <summary>
	/// Gets the metadata tag of the tag endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The tag endpoint's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.Tag];
	}

	/// <summary>
	/// Collects the IDs from <c>-Id</c>, and the IDs and labels from <c>-Name</c>.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "<Pending>")]
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
		if (this.HasParameter(this.Name))
		{
			this.Name.SplitToSets(_ids, _wcNames, !this.MyInvocation.IsBoundPositionally(nameof(Name)));
		}
	}
	/// <summary>
	/// Adds the IDs of the tags that the piped objects carry, those greater than zero, to the IDs to get.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		if (this.HasParameter(this.InputObject))
		{
			_ids.UnionWith(this.InputObject.SelectMany(x => x.Tags)
										   .Where(x => x > 0));
		}
	}
	/// <summary>
	/// Gets the tags and writes them to the pipeline: each ID's tag when only IDs were collected, the tags that match
	/// the IDs or labels when input or <c>-Name</c> was given, and every tag otherwise.
	/// </summary>
	/// <remarks>
	/// Piped objects or a <c>-Name</c> that gave no ID or label get no tags. When the request for every tag fails, the
	/// cmdlet stops with its error.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		IList<TagObject> tags = _ids.Count > 0 && _wcNames.Count == 0
			? this.GetById<TagObject>(_ids)
			: this.ParameterSetName == PSConstants.PSetPipeline || this.HasParameter(this.Name)
				? this.GetAllAndFilter<TagObject>(_ids, _wcNames)
				: this.GetAll<TagObject>();

		this.WriteCollection(tags);
	}
}
