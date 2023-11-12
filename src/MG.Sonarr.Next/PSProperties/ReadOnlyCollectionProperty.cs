using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties
{
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

