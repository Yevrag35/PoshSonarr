using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Pwsh;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags;

/// <summary>
/// Represents the <c>Add-SonarrTag</c> cmdlet, which adds tags to the Sonarr objects that are piped to it.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes objects that carry tags, such as series, delay profiles, download clients, indexers, and release
/// profiles, through <c>-InputObject</c> as <see cref="ITagPipeable"/> values, and collects them until the pipeline
/// ends. An object piped more than once is changed once.
/// </para>
/// <para>
/// <c>-Id</c> gives the tags to add by ID, and <c>-Name</c> by ID or label. When names are given, the cmdlet gets every
/// tag from Sonarr with one request, and when that request fails, it stops with its error.
/// </para>
/// <para>
/// For each object that lacks one of the tags, the cmdlet confirms the change, naming the object's endpoint and the
/// IDs that it adds, adds the tags, and sends the object back to Sonarr with a PUT request, which is serialized by the
/// object's runtime type. An object that doesn't exist on the server yet keeps the tags without a request, and they go
/// out with the request that creates it. When the request fails, the object gets its earlier tags back and the cmdlet
/// writes the error, unless it's ignorable. The cmdlet writes nothing to the pipeline, and an object that already has
/// every tag gets a verbose message instead of a request.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Add, "SonarrTag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
	DefaultParameterSetName = "None")]
[MetadataCanPipe(Tag = Meta.DelayProfile)]
[MetadataCanPipe(Tag = Meta.DownloadClient)]
[MetadataCanPipe(Tag = Meta.Indexer)]
[MetadataCanPipe(Tag = Meta.ReleaseProfile)]
[MetadataCanPipe(Tag = Meta.Series)]
[MetadataCanPipe(Tag = Meta.SeriesAdd)]
public sealed class AddSonarrTagCmdlet : SonarrMetadataCmdlet
{
	static readonly string _namePropertyName = nameof(Name);
	SortedSet<int> _ids = null!;
	WildcardSet _wcNames = null!;
	Dictionary<string, ITagPipeable> _updates = null!;

	/// <summary>
	/// Gets or sets the objects that the tags are added to.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input. Each object has to implement <see cref="ITagPipeable"/> and have an ID
	/// greater than zero.
	/// </remarks>
	/// <value>The objects that carry tags. The default is an empty array.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline)]
	[ValidateIds<ITagPipeable>(ValidateRangeKind.Positive)]
	public ITagPipeable[] InputObject { get; set; } = [];

	/// <summary>
	/// Gets or sets the IDs of the tags to add.
	/// </summary>
	/// <remarks>
	/// When IDs are given, the cmdlet doesn't read <c>-Name</c>.
	/// </remarks>
	/// <value>The tag IDs. The default is an empty array.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSetExplicitId)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets the IDs and labels of the tags to add.
	/// </summary>
	/// <remarks>
	/// Given by position, a number, or text that reads as a whole number, is a tag's ID, and any other text is a label,
	/// which can hold wildcards and matches ignoring case. Given by name, as <c>-Name</c>, every value is a label, a
	/// number as its text. A label that matches no tag adds nothing.
	/// </remarks>
	/// <value>The IDs and labels of the tags. The default is an empty array.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Position = 0)]
	public Either<string, int>[] Name { get; set; } = [];

	/// <summary>
	/// Runs the base class's setup for the scope, then creates the sets of tag IDs and labels and the map of objects to
	/// update, keyed by endpoint path ignoring case.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = [];
		_wcNames = new();
		_updates = new(5, StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Adds the specified objects to the map of objects to update, keyed by the path of each object's endpoint, and
	/// skips an object whose path is already there.
	/// </summary>
	/// <param name="pipeables">The objects that were piped to the cmdlet.</param>
	private void AddUrlsFromMetadata(ITagPipeable[] pipeables)
	{
		foreach (ITagPipeable item in pipeables)
		{
			bool added = _updates.TryAdd(item.MetadataTag.GetUrlForId(item.Id), item);
		}
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
	/// Collects the IDs of the tags to add, from <c>-Id</c>, or else from <c>-Name</c>, along with the IDs of the tags
	/// whose labels match its names.
	/// </summary>
	/// <remarks>
	/// Matching the names gets every tag with one request, and when that request fails, the cmdlet stops with its error.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Begin(IServiceProvider provider)
	{
		if (this.Id.Length > 0)
		{
			_ids.UnionWith(this.Id);
		}
		else if (this.Name.Length > 0)
		{
			this.Name.SplitToSets(_ids, _wcNames, !this.MyInvocation.IsBoundPositionally(_namePropertyName));
		}

		if (_wcNames.Count > 0)
		{
			var all = this.GetAll<TagObject>();

			foreach (TagObject tag in all)
			{
				if (_wcNames.IsAnyMatch(tag.Label))
				{
					_ = _ids.Add(tag.Id);
				}
			}
		}
	}
	/// <summary>
	/// Adds the piped objects to the ones that the cmdlet updates when the pipeline ends.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		if (this.InputObject.Length > 0)
		{
			this.AddUrlsFromMetadata(this.InputObject);
		}
	}
	/// <summary>
	/// Adds the tags to each collected object that lacks one of them, once the change is confirmed, and saves the
	/// object to Sonarr when it exists there.
	/// </summary>
	/// <remarks>
	/// An object that already has every tag gets a verbose message and no request. A failed request restores the
	/// object's earlier tags and writes its error, unless the error is ignorable, and the cmdlet goes on to the next
	/// object.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		foreach (KeyValuePair<string, ITagPipeable> kvp in _updates)
		{
			if (kvp.Value.Tags.IsSupersetOf(_ids))
			{
				this.WriteVerbose("No tags are being added that didn't already exist on the object.");
				continue;
			}

			if (this.ShouldProcess(
				target: kvp.Key,
				action: string.Format(
					CultureInfo.CurrentCulture,
					"Adding tags: ({0})",
					string.Join(", ", _ids.Where(x => !kvp.Value.Tags.Contains(x))))))
			{
				if (!this.PerformTagUpdate(kvp))
				{
					continue;
				}
			}
		}
	}

	/// <summary>
	/// Adds the tags to the specified object and, when the object exists on the server, sends it to its endpoint with
	/// a PUT request.
	/// </summary>
	/// <remarks>
	/// When the request succeeds, the object commits its tags, and when it fails, the object resets them and the
	/// method writes the error, unless it's ignorable. An object that doesn't exist on the server keeps the tags
	/// without a request or a commit.
	/// </remarks>
	/// <param name="kvp">The path of the object's endpoint and the object.</param>
	/// <returns>
	/// <see langword="true"/> if the object has the tags afterward; otherwise, <see langword="false"/>, when the request
	/// failed.
	/// </returns>
	private bool PerformTagUpdate(KeyValuePair<string, ITagPipeable> kvp)
	{
		kvp.Value.Tags.UnionWith(_ids);

		if (!kvp.Value.MustUpdateViaApi)
		{
			return true;
		}

		var response = this.SendPutRequest(path: kvp.Key, body: kvp.Value);
		if (response.IsError)
		{
			kvp.Value.Reset();
			this.WriteConditionalError(response.Error);
			return false;
		}
		else
		{
			kvp.Value.Commit();
			return true;
		}
	}
}
