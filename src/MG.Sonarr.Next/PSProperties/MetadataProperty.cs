using System.Management.Automation;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.PSProperties
{
    public sealed class MetadataProperty : ReadOnlyProperty<MetadataTag>
    {
        static readonly string _typeName = typeof(MetadataTag).GetTypeName();
        public MetadataTag Tag { get; internal set; }
        public override string TypeNameOfValue => _typeName;
        protected override MetadataTag ValueAsT => this.Tag;

        private MetadataProperty()
            : this(MetadataTag.Empty)
        {
        }
        public MetadataProperty(MetadataTag value)
            : base(MetadataResolver.META_PROPERTY_NAME)
        {
            this.Tag = value;
        }

        internal static readonly MetadataProperty Empty = new();

        public override PSMemberInfo Copy()
        {
            return this;
        }
    }
}
