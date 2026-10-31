using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.PSProperties;

namespace MG.Sonarr.Next.Models;

/// <summary>
/// Represents a Sonarr API resource that has a numeric ID and carries tag IDs.
/// </summary>
/// <remarks>
/// <para>
/// The object can be piped as an <see cref="ITagPipeable"/> to the cmdlets that read and change tags. Its
/// <see cref="Tags"/> is the set that the read-only <c>Tags</c> note property holds, so a change made to it in C#
/// shows in PowerShell and in the next request body, while PowerShell can't replace the set.
/// </para>
/// <para>
/// <see cref="Commit"/> and <see cref="Reset"/> are sealed. A derived model adds to them through
/// <see cref="OnCommit"/> and <see cref="OnReset"/>, and to deserialization through an override of
/// <see cref="OnDeserialized(bool)"/> that calls the base implementation first.
/// </para>
/// </remarks>
/// <typeparam name="TSelf">The derived model type.</typeparam>
public abstract class TagUpdateObject<TSelf> : IdSonarrObject<TSelf>,
	ITagPipeable
	where TSelf : IdSonarrObject<TSelf>
{
	/// <summary>
	/// The object's tag IDs, or <see langword="null"/> until they're set or first read.
	/// </summary>
	private SortedSet<int>? _tags;
	/// <summary>
	/// The snapshot of tag IDs that <see cref="Reset"/> puts back, or <see langword="null"/> if none was taken.
	/// </summary>
	private int[]? _originalTags;
	//private int _originalTagCount;

	/// <summary>
	/// Gets or sets a value that indicates whether a change to the object's tags must be saved with a request to Sonarr.
	/// </summary>
	/// <remarks>
	/// A tag cmdlet sends the object back with a PUT after changing its tags only when this is <see langword="true"/>.
	/// <see cref="OnDeserialized(bool)"/> sets it to <see langword="true"/>, so a model for objects that don't exist
	/// on the server yet sets it to <see langword="false"/> after calling the base implementation.
	/// </remarks>
	/// <value>
	/// <see langword="true"/> if the object exists on the server; <see langword="false"/> if it doesn't exist there
	/// yet, such as a lookup result or a schema, whose tags are sent with the request that creates it.
	/// </value>
	public virtual bool MustUpdateViaApi { get; protected set; }

	/// <summary>
	/// Gets or sets the object's tag IDs.
	/// </summary>
	/// <remarks>
	/// Setting the property takes a snapshot of the set's IDs, which <see cref="Reset"/> puts back. Reading it before
	/// it's set gives a new empty set.
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown when the property is set to null.</exception>
	/// <value>The set of the object's tag IDs, in ascending order.</value>
	public SortedSet<int> Tags
	{
		get => _tags ??= [];
		set
		{
			ArgumentNullException.ThrowIfNull(value, nameof(this.Tags));
			_tags = value;
			_originalTags = [.. value];
		}
	}
	/// <summary>
	/// Gets the object's tag IDs.
	/// </summary>
	/// <value>The set that <see cref="Tags"/> returns.</value>
	ISet<int> ITagPipeable.Tags => this.Tags;

	/// <summary>
	/// Initializes a new <see cref="TagUpdateObject{TSelf}"/> instance with room for the specified number of members.
	/// </summary>
	/// <param name="capacity">
	/// The number of members that the object's member collection starts with room for, about the number of fields that
	/// Sonarr sends for the resource.
	/// </param>
	protected TagUpdateObject(int capacity)
		: base(capacity)
	{
	}

	/// <summary>
	/// Makes the object's current tags its snapshot and brings the rest of the object back in step after a request
	/// that sends it succeeds.
	/// </summary>
	/// <remarks>
	/// It calls <see cref="ITagPipeable.CommitTags"/> and then <see cref="OnCommit"/>.
	/// </remarks>
	public sealed override void Commit()
	{
		((ITagPipeable)this).CommitTags();
		this.OnCommit();
	}
	//BUG: CommitTags does nothing (its body is commented out), so a Reset after a successful Commit restores the tags from before the commit.
	/// <summary>
	/// Makes the object's current tags the snapshot that <see cref="Reset"/> puts back.
	/// </summary>
	void ITagPipeable.CommitTags()
	{
		if (_originalTags is not null)
		{
			//_originalTags.Clear();
			//_originalTags.AddRange(this.Tags);
		}
	}

	/// <summary>
	/// When overridden in a derived class, brings the object's other values back in step after a request that sends it
	/// succeeds.
	/// </summary>
	/// <remarks>
	/// <see cref="Commit"/> calls it after committing the tags. The base implementation does nothing.
	/// </remarks>
	protected virtual void OnCommit()
	{
		return;
	}

	/// <summary>
	/// Marks the object as one that exists on the server and takes its tag IDs from its <c>Tags</c> note property.
	/// </summary>
	/// <remarks>
	/// It sets <see cref="MustUpdateViaApi"/> to <see langword="true"/>, and when the object has a
	/// <see cref="ReadOnlyTagsProperty"/> named <c>Tags</c>, sets <see cref="Tags"/> to the set it holds, which takes
	/// the snapshot. A derived model's override calls it first.
	/// </remarks>
	/// <param name="alreadyCalled">
	/// <see langword="true"/> if <see cref="SonarrObject.OnDeserialized()"/> has run on the object before; otherwise,
	/// <see langword="false"/>.
	/// </param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		this.MustUpdateViaApi = true;
		if (this.Properties[Constants.TAGS] is ReadOnlyTagsProperty tagsProp)
		{
			this.Tags = tagsProp.Tags;
		}
	}

	/// <summary>
	/// Puts the snapshot of the object's tags back and brings the rest of the object back to its state before a
	/// request that sends it failed.
	/// </summary>
	/// <remarks>
	/// The set itself is kept, so the <c>Tags</c> note property shows the restored IDs. When no snapshot was taken, the
	/// set is cleared. It then calls <see cref="OnReset"/>.
	/// </remarks>
	public sealed override void Reset()
	{
		if (_tags is not null)
		{
			_tags.Clear();
			if (_originalTags is not null)
			{
				_tags.UnionWith(_originalTags);
			}
		}

		this.OnReset();
	}

	/// <summary>
	/// When overridden in a derived class, brings the object's other values back to their state before a request that
	/// sends it failed.
	/// </summary>
	/// <remarks>
	/// <see cref="Reset"/> calls it after restoring the tags. The base implementation does nothing.
	/// </remarks>
	protected virtual void OnReset()
	{
		return;
	}

	/// <summary>
	/// Gets the ID that the tag cmdlets act on.
	/// </summary>
	/// <returns>The object's <see cref="IdSonarrObject{TSelf}.Id"/>.</returns>
	int? IPipeable<ITagPipeable>.GetId()
	{
		return this.Id;
	}
}
