using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties
{
    public sealed class ReadOnlyCollectionProperty<T> : ReadOnlyProperty<IReadOnlyList<T>>
    {
        public IReadOnlyList<T> Collection { get; }
        protected override IReadOnlyList<T> ValueAsT => this.Collection;

        public ReadOnlyCollectionProperty(string propertyName, IReadOnlyList<T>? list)
            : base(propertyName)
        {
            this.Collection = list ?? Array.Empty<T>();
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
            this.Collection = collection ?? new();
        }

        public override PSMemberInfo Copy()
        {
            return this;
        }
    }
}

