using System.Collections.Immutable;
using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties
{
    public sealed class ReadOnlyCollectionProperty<T> : ReadOnlyProperty<ImmutableArray<T>>
    {
        public ImmutableArray<T> Collection { get; }
        protected override ImmutableArray<T> ValueAsT => this.Collection;

        public ReadOnlyCollectionProperty(string propertyName, ImmutableArray<T> list)
            : base(propertyName)
        {
            this.Collection = list;
        }

        public override PSMemberInfo Copy()
        {
            return this;
        }
    }

    public sealed class ReadOnlyCollectionProperty<T, TCol> : ReadOnlyProperty<TCol>
        where TCol : IEnumerable<T>, new()
    {
        public TCol Collection { get; }
        protected override TCol ValueAsT => this.Collection;

        public ReadOnlyCollectionProperty(string propertyName, TCol? collection)
            : base(propertyName)
        {
            this.Collection = collection ?? [];
        }

        public override PSMemberInfo Copy()
        {
            return this;
        }
    }
}

