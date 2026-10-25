using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.PSProperties;

namespace MG.Sonarr.Next.Models
{
 /// <summary>
 /// Provides a base type for Sonarr objects that support tag updates and piping to the tag endpoint.
 /// </summary>
 /// <remarks>
 /// Derive from this class to add tag management for objects that implement tag-based updates. The generic
 /// parameter <typeparamref name="TSelf"/> must be a subtype of <see cref="IdSonarrObject{TSelf}"/> which
 /// ensures the implementation exposes an <see cref="IHasId"/>-style identifier.
 /// </remarks>
 /// <typeparam name="TSelf">The implementing type that derives from <see cref="IdSonarrObject{TSelf}"/>.</typeparam>
 public abstract class TagUpdateObject<TSelf> : IdSonarrObject<TSelf>,
 ITagPipeable
 where TSelf : IdSonarrObject<TSelf>
 {
 private SortedSet<int>? _tags;
 private int[]? _originalTags;

 /// <summary>
 /// Gets or sets a value indicating whether tag changes must be persisted via the Sonarr API.
 /// </summary>
 /// <remarks>
 /// When <see langword="true"/>, callers should use the Sonarr API to apply tag changes rather than relying on local operations.
 /// The setter is <c>protected</c> to allow derived types to control when an object requires API updates.
 /// </remarks>
 /// <value><see langword="true"/> if updates must be saved via the API; otherwise, <see langword="false"/>.</value>
 public virtual bool MustUpdateViaApi { get; protected set; }

 /// <summary>
 /// Gets or sets the unique set of tag identifiers associated with this object.
 /// </summary>
 /// <remarks>
 /// The returned collection is a <see cref="SortedSet{T}"/> of tag IDs. The setter copies the contents
 /// into an internal array to preserve the original tag state for later commit/reset operations.
 /// </remarks>
 /// <value>A mutable <see cref="SortedSet{Int32}"/> containing the tag IDs for this object.</value>
 /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="value"/> is <see langword="null"/>.</exception>
 public SortedSet<int> Tags
 {
 get => _tags ??= [];
 set
 {
 ArgumentNullException.ThrowIfNull(value, nameof(this.Tags));
 _tags = value;
 _originalTags = new int[value.Count];
 value.CopyTo(_originalTags);
 }
 }

 /// <summary>
 /// Gets the set of tags via the <see cref="ITagPipeable"/> interface.
 /// </summary>
 /// <remarks>
 /// This explicit interface implementation forwards to <see cref="Tags"/> and exposes the set as an <see cref="ISet{Int32}"/>.
 /// </remarks>
 ISet<int> ITagPipeable.Tags => this.Tags;

 /// <summary>
 /// Initializes a new instance of the <see cref="TagUpdateObject{TSelf}"/> class with the specified capacity.
 /// </summary>
 /// <remarks>
 /// The <paramref name="capacity"/> is passed to the base <see cref="SonarrObject"/> ctor to size internal collections.
 /// </remarks>
 /// <param name="capacity">The initial capacity for internal storage.</param>
 protected TagUpdateObject(int capacity)
 : base(capacity)
 {
 }

 /// <summary>
 /// Commit tag changes and run derived commit logic.
 /// </summary>
 /// <remarks>
 /// This sealed override first commits tags via the <see cref="ITagPipeable.CommitTags"/> implementation
 /// and then calls <see cref="OnCommit"/> for additional derived-type behavior.
 /// </remarks>
 public sealed override void Commit()
 {
 ((ITagPipeable)this).CommitTags();
 this.OnCommit();
 }

 void ITagPipeable.CommitTags()
 {
 if (_originalTags is not null)
 {
 Array.Clear(_originalTags);
 if (_originalTags.Length != this.Tags.Count)
 {
 Array.Resize(ref _originalTags, this.Tags.Count);
 }

 this.Tags.CopyTo(_originalTags);
 }
 }

 /// <summary>
 /// Called after tags have been committed to perform derived commit actions.
 /// </summary>
 /// <remarks>
 /// Default implementation does nothing. Override to clear caches or update derived state after a successful commit.
 /// </remarks>
 protected virtual void OnCommit()
 {
 return;
 }

 /// <summary>
 /// Handle additional deserialization work specific to tag updates.
 /// </summary>
 /// <remarks>
 /// Sets <see cref="MustUpdateViaApi"/> to <see langword="true"/> and, if a <see cref="ReadOnlyTagsProperty"/>
 /// is present in <see cref="PSObject.Properties"/>, initializes <see cref="Tags"/> from it.
 /// </remarks>
 /// <param name="alreadyCalled"><see langword="true"/> if deserialization hooks have already run; otherwise <see langword="false"/>.</param>
 protected override void OnDeserialized(bool alreadyCalled)
 {
 this.MustUpdateViaApi = true;
 if (this.Properties[Constants.TAGS] is ReadOnlyTagsProperty tagsProp)
 {
 this.Tags = tagsProp.Tags;
 }
 }

 /// <summary>
 /// Reset tags to their original state and run derived reset logic.
 /// </summary>
 /// <remarks>
 /// If no original tag snapshot exists, the tags collection is cleared. After restoring the original values
 /// this method calls <see cref="OnReset"/> for further derived behavior.
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
 /// Called after Reset to allow derived types to restore or clear additional state.
 /// </summary>
 /// <remarks>
 /// Default implementation does nothing. Override to revert non-tag state to its original values.
 /// </remarks>
 protected virtual void OnReset()
 {
 return;
 }

 int? IPipeable<ITagPipeable>.GetId()
 {
 return this.Id;
 }
 }
}
