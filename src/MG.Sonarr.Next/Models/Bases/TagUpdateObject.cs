using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Metadata;

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

        public override void Commit()
        {
            base.Commit();
            this.CommitTags();
        }
        public void CommitTags()
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
        public override void OnDeserialized()
        {
            base.OnDeserialized();
            this.MustUpdateViaApi = true;
            if (this.TryGetNonNullProperty(nameof(this.Tags), out SortedSet<int>? tags))
            {
                this.Tags = tags;
            }
        }
        public override void Reset()
        {
            _tags?.Clear();
            _tags?.UnionWith(_originalTags ?? Array.Empty<int>());
            base.Reset();
        }
    }
}
