using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Pwsh;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags;

/// <summary>
/// Represents the <c>Clear-SonarrTag</c> cmdlet, which removes tags from the Sonarr objects that are piped to it.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes objects that carry tags, such as series, delay profiles, download clients, indexers, notifications,
/// and release profiles, through <c>-InputObject</c> as <see cref="ITagPipeable"/> values, and collects them until the
/// pipeline ends. It also takes objects that don't exist on the server yet, such as series lookup results and download
/// client and notification schemas. An object piped more than once is changed once, and of two copies of the same
/// object on the server, only the first one piped is changed.
/// </para>
/// <para>
/// <c>-Id</c> gives the tags to remove by ID, and <c>-Name</c> by ID or label. When names are given, the cmdlet gets
/// every tag from Sonarr with one request, and when that request fails, it stops with its error.
/// </para>
/// <para>
/// For each object that carries one of the tags, the cmdlet confirms the change, naming the object's endpoint and the
/// IDs that it removes, removes the tags, and sends the object back to Sonarr with a PUT request, which is serialized
/// by the object's runtime type. An object that doesn't exist on the server yet loses the tags without a request, so
/// the request that creates it goes out without them. When the request fails, the object gets its earlier tags back
/// and the cmdlet writes the error, unless it's ignorable. The cmdlet writes nothing to the pipeline, and an object
/// that carries none of the tags gets a verbose message instead of a request.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Clear, "SonarrTag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
	DefaultParameterSetName = "None")]
[MetadataCanPipe(Tag = Meta.DelayProfile)]
[MetadataCanPipe(Tag = Meta.DownloadClient)]
[MetadataCanPipe(Tag = Meta.DownloadClientSchema)]
[MetadataCanPipe(Tag = Meta.Indexer)]
[MetadataCanPipe(Tag = Meta.Notification)]
[MetadataCanPipe(Tag = Meta.ReleaseProfile)]
[MetadataCanPipe(Tag = Meta.Series)]
[MetadataCanPipe(Tag = Meta.SeriesAdd)]
public sealed class ClearSonarrTagCmdlet : SonarrMetadataCmdlet
{
	static readonly string _namePropertyName = nameof(Name);
	SortedSet<int> _ids = null!;
	WildcardSet _wcNames = null!;
	Dictionary<string, ITagPipeable> _updates = null!;
	HashSet<ITagPipeable> _unsaved = null!;

	/// <summary>
	/// Gets or sets the objects that the tags are removed from.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, and each object has to implement <see cref="ITagPipeable"/>. An object that
	/// exists on the server needs an ID greater than zero, or the cmdlet writes an error and leaves it unchanged, while
	/// one that doesn't exist there yet, such as a series lookup result, needs no ID.
	/// </remarks>
	/// <value>The objects that carry tags. The default is an empty array.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline)]
	public ITagPipeable[] InputObject { get; set; } = [];

	/// <summary>
	/// Gets or sets the IDs of the tags to remove.
	/// </summary>
	/// <remarks>
	/// When IDs are given, the cmdlet doesn't read <c>-Name</c>.
	/// </remarks>
	/// <value>The tag IDs. The default is an empty array.</value>
	[Parameter(ParameterSetName = PSConstants.PSetPipeline)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets the IDs and labels of the tags to remove.
	/// </summary>
	/// <remarks>
	/// Given by position, a number, or text that reads as a whole number, is a tag's ID, and any other text is a label,
	/// which can hold wildcards and matches ignoring case. Given by name, as <c>-Name</c>, every value is a label, a
	/// number as its text. A label that matches no tag removes nothing.
	/// </remarks>
	/// <value>The IDs and labels of the tags. The default is an empty array.</value>
	[Parameter(Position = 0)]
	public Either<string, int>[] Name { get; set; } = [];

	/// <summary>
	/// Runs the base class's setup for the scope, then creates the sets of tag IDs and labels, the map of objects on the
	/// server to update, keyed by endpoint path ignoring case, and the set of objects that don't exist there yet.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = [];
		_wcNames = new();
		_updates = new(5, StringComparer.OrdinalIgnoreCase);
		_unsaved = new(ReferenceEqualityComparer.Instance);
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
	/// Collects the IDs of the tags to remove, from <c>-Id</c>, or else from <c>-Name</c>, along with the IDs of the
	/// tags whose labels match its names.
	/// </summary>
	/// <remarks>
	/// Matching the names gets every tag with one request, and when that request fails, the cmdlet stops with its error.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
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

			foreach (var tag in all)
			{
				if (_wcNames.IsAnyMatch(tag.Label))
				{
					_ = _ids.Add(tag.Id);
				}
			}
		}
	}
	/// <summary>
	/// Collects the piped objects that the cmdlet changes when the pipeline ends: an object that exists on the server by
	/// the path of its endpoint, and one that doesn't exist there yet by reference.
	/// </summary>
	/// <remarks>
	/// An object that exists on the server but has no ID greater than zero gets an error and isn't collected, and the
	/// cmdlet goes on to the next object.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		foreach (ITagPipeable item in this.InputObject)
		{
			if (!item.MustUpdateViaApi)
			{
				_ = _unsaved.Add(item);
			}
			else if (item.Id > 0)
			{
				_ = _updates.TryAdd(item.MetadataTag.GetUrlForId(item.Id), item);
			}
			else
			{
				this.WriteError(new ArgumentException(
					"An object that exists on the server needs an ID greater than zero for its tags to be saved.",
					nameof(this.InputObject))
						.ToRecord(ErrorCategory.InvalidArgument, item));
			}
		}
	}
	/// <summary>
	/// Removes the tags from each collected object that carries one of them, once the change is confirmed, and saves
	/// each object that exists on the server to Sonarr.
	/// </summary>
	/// <remarks>
	/// The objects that exist on the server go first, and then the ones that don't, which lose the tags without a
	/// request. An object that carries none of the tags gets a verbose message and no request. When the request
	/// succeeds, the object commits its tags, and when it fails, the object resets them and the cmdlet writes the error,
	/// unless it's ignorable, and goes on to the next object.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		foreach (var (path, item) in _updates)
		{
			if (this.RemoveTagsIfConfirmed(item, target: path))
			{
				this.SaveTags(path, item);
			}
		}

		foreach (ITagPipeable item in _unsaved)
		{
			_ = this.RemoveTagsIfConfirmed(item, target: $"{item.MetadataTag.UrlBase} (unsaved object)");
		}
	}

	/// <summary>
	/// Removes the tags that the specified object carries, once the change is confirmed.
	/// </summary>
	/// <remarks>
	/// The confirmation names the target and the IDs that the object carries. An object that carries none of the tags
	/// gets a verbose message instead.
	/// </remarks>
	/// <param name="item">The object to remove the tags from.</param>
	/// <param name="target">
	/// The name of the object that the confirmation shows: the path of its endpoint, or, for an object that doesn't exist
	/// on the server yet, the endpoint's path with a note that it's unsaved.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the tags were removed; otherwise, <see langword="false"/>, when the object carries none
	/// of the tags or the change isn't confirmed.
	/// </returns>
	private bool RemoveTagsIfConfirmed(ITagPipeable item, string target)
	{
		if (!_ids.Overlaps(item.Tags))
		{
			this.WriteVerbose("No tags are being removed from the object.");
			return false;
		}

		string tagIds = string.Join(", ", _ids.Where(item.Tags.Contains));
		if (!this.ShouldProcess(target, $"Removing tags: ({tagIds})"))
		{
			return false;
		}

		item.Tags.ExceptWith(_ids);
		return true;
	}

	/// <summary>
	/// Sends the specified object to its endpoint with a PUT request, which saves its changed tags.
	/// </summary>
	/// <remarks>
	/// When the request succeeds, the object commits its tags, and when it fails, the object resets them and the method
	/// writes the error, unless it's ignorable.
	/// </remarks>
	/// <param name="path">The path of the object's endpoint.</param>
	/// <param name="item">The object, which exists on the server.</param>
	private void SaveTags(string path, ITagPipeable item)
	{
		var response = this.SendPutRequest(path: path, body: item);
		if (response.IsError)
		{
			item.Reset();
			this.WriteConditionalError(response.Error);
		}
		else
		{
			item.Commit();
		}
	}
}
