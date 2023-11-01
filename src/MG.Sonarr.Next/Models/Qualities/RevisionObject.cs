using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Qualities
{
    public sealed class RevisionObject : SonarrObject,
        IComparable<RevisionObject>,
        ISerializableNames<RevisionObject>
    {
        const int CAPACITY = 3;
        static readonly string _typeName = typeof(RevisionObject).GetTypeName();
        readonly bool _wasCtored;

        public bool IsRepack
        {
            get => this.GetValue<bool>();
            private set => this.ReplaceStructProperty(nameof(this.IsRepack), value, isReadOnly: true);
        }
        public int Real
        {
            get => this.GetValue<int>();
            set => this.ReplaceNumberProperty(nameof(this.Real), value, isReadOnly: true);
        }
        public int Version
        {
            get => this.GetValue<int>();
            set => this.ReplaceNumberProperty(nameof(this.Version), value, isReadOnly: true);
        }

        public RevisionObject()
            : base(CAPACITY)
        {
        }
        private RevisionObject(bool isProper, bool isReal)
            : this()
        {
            _wasCtored = true;
            this.IsRepack = false;
            this.Version = isProper ? 2 : 1;
            this.Real = isReal ? 1 : 0;
        }

        public int CompareTo(RevisionObject? other)
        {
            return _wasCtored.CompareTo(other?._wasCtored);
        }
        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.REVISION];
        }
        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }

        public static RevisionObject Create(bool isProper, bool isReal)
        {
            if (!isProper && !isReal)
            {
                return Default;
            }

            return new(isProper, isReal);
        }
        public static RevisionObject Default => new(false, false);
    }
}

