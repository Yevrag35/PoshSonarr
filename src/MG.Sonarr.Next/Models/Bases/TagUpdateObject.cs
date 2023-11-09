using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.PSProperties;

namespace MG.Sonarr.Next.Models
{
    public abstract class TagUpdateObject<TSelf> : IdSonarrObject<TSelf>,
        ITagPipeable
        where TSelf : IdSonarrObject<TSelf>
    {
        private SortedSet<int>? _tags;
        private int[]? _originalTags;

        public virtual bool MustUpdateViaApi { get; protected set; }
        public SortedSet<int> Tags
        {
            get => _tags ??= new();
            set
            {
                ArgumentNullException.ThrowIfNull(value, nameof(this.Tags));
                _tags = value;
                _originalTags = new int[value.Count];
                value.CopyTo(_originalTags);
            }
        }
        ISet<int> ITagPipeable.Tags => this.Tags;

        protected TagUpdateObject(int capacity)
            : base(capacity)
        {
        }

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
        protected virtual void OnCommit()
        {
            return;
        }
        protected override void OnDeserialized(bool alreadyCalled)
        {
            this.MustUpdateViaApi = true;
            if (this.Properties[Constants.TAGS] is ReadOnlyTagsProperty tagsProp)
            {
                this.Tags = tagsProp.Tags;
            }
        }
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
