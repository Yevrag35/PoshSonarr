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
/// Represents the <c>Clear-SonarrTag</c> cmdlet, which removes tags from the Sonarr objects that are piped to it.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes objects that carry tags, such as series, delay profiles, download clients, indexers, and release
/// profiles, through <c>-InputObject</c> as <see cref="ITagPipeable"/> values, and collects them until the pipeline
/// ends. An object piped more than once is changed once.
/// </para>
/// <para>
/// <c>-Id</c> gives the tags to remove by ID, and <c>-Name</c> by ID or label. When names are given, the cmdlet gets
/// every tag from Sonarr with one request, and when that request fails, it stops with its error.
/// </para>
/// <para>
/// For each object that carries one of the tags, the cmdlet confirms the change, naming the object's endpoint and the
/// IDs that it removes, removes the tags, and sends the object back to Sonarr with a PUT request, which is serialized
/// by the object's runtime type. When the request fails, the object gets its earlier tags back and the cmdlet writes
/// the error, unless it's ignorable. The cmdlet writes nothing to the pipeline, and an object that carries none of the
/// tags gets a verbose message instead of a request.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Clear, "SonarrTag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
	DefaultParameterSetName = "None")]
[MetadataCanPipe(Tag = Meta.DelayProfile)]
[MetadataCanPipe(Tag = Meta.DownloadClient)]
[MetadataCanPipe(Tag = Meta.Indexer)]
[MetadataCanPipe(Tag = Meta.ReleaseProfile)]
[MetadataCanPipe(Tag = Meta.Series)]
[MetadataCanPipe(Tag = Meta.SeriesAdd)]
public sealed class ClearSonarrTagCmdlet : SonarrMetadataCmdlet
{
	static readonly string _namePropertyName = nameof(Name);
	SortedSet<int> _ids = null!;
	WildcardSet _wcNames = null!;
	Dictionary<string, ITagPipeable> _updates = null!;

	/// <summary>
	/// Gets or sets the objects that the tags are removed from.
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
	/// Gets or sets the IDs of the tags to remove.
	/// </summary>
	/// <remarks>
	/// When IDs are given, the cmdlet doesn't read <c>-Name</c>.
	/// </remarks>
	/// <value>The tag IDs. The default is an empty array.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSetExplicitId)]
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
	/// Adds the specified objects to the specified map of objects to update, keyed by the path of each object's
	/// endpoint, and skips an object whose path is already there.
	/// </summary>
	/// <remarks>
	/// The check for an empty map is meant to report piped input that gave no object to update.
	/// </remarks>
	/// <param name="array">The objects that were piped to the cmdlet.</param>
	/// <param name="updates">The map of objects to update, which the method adds to.</param>
	/// <exception cref="ArgumentException">Thrown when the map is empty after the objects are added.</exception>
	private static void AddUrlsFromMetadata(ITagPipeable[] array, Dictionary<string, ITagPipeable> updates)
	{
		foreach (ITagPipeable pipeable in array)
		{
			updates.TryAdd(pipeable.MetadataTag.GetUrlForId(pipeable.Id), pipeable);
		}

		if (updates.Count == 0)
		{
			throw new ArgumentException(
				"No tag ID's were provided from the pipeline. Did you mean to use \"Clear-SonarrTag\"?");
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
	/// Adds the piped objects to the ones that the cmdlet updates when the pipeline ends.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		if (this.InputObject.Length > 0)
		{
			AddUrlsFromMetadata(this.InputObject, _updates);
		}
	}
	/// <summary>
	/// Removes the tags from each collected object that carries one of them, once the change is confirmed, and saves
	/// the object to Sonarr.
	/// </summary>
	/// <remarks>
	/// An object that carries none of the tags gets a verbose message and no request. When the request succeeds, the
	/// object commits its tags, and when it fails, the object resets them and the cmdlet writes the error, unless it's
	/// ignorable, and goes on to the next object.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		foreach (var kvp in _updates)
		{
			if (!_ids.Overlaps(kvp.Value.Tags))
			{
				this.WriteVerbose("No tags are being removed from the object.");
				continue;
			}

			if (this.ShouldProcess(
				target: kvp.Key,
				action: string.Format(
					CultureInfo.CurrentCulture,
					"Removing tags: ({0})",
					string.Join(", ", _ids.Where(kvp.Value.Tags.Contains)))))
			{
				kvp.Value.Tags.ExceptWith(_ids);

				var response = this.SendPutRequest(path: kvp.Key, body: kvp.Value);
				if (response.IsError)
				{
					kvp.Value.Reset();
					this.WriteConditionalError(response.Error);
				}
				else
				{
					kvp.Value.Commit();
				}
			}
		}
	}
}
