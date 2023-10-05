using System.Management.Automation;

namespace MG.Sonarr.Next.Services.Metadata
{
    public sealed class MetadataProperty : PSNoteProperty
    {
        MetadataTag? _tag;

        public override bool IsGettable => true;
        public override bool IsSettable => true;
        public override PSMemberTypes MemberType => PSMemberTypes.NoteProperty;
        public MetadataTag Tag
        {
            get => _tag ??= MetadataTag.Empty;
            set
            {
                base.Value = value;
                _tag = value;
            }
        }
        public override object Value
        {
            get => this.Tag;
            set
            {
                value ??= MetadataTag.Empty;
                if (value is not MetadataTag tag)
                {
                    throw new ArgumentException("value must be of type 'MetadataTag'.");
                }

                this.Tag = tag;
            }
        }

        public MetadataProperty(MetadataTag value)
            : base(MetadataResolver.META_PROPERTY_NAME, value)
        {
            _tag = value;
        }

        public override PSMemberInfo Copy()
        {
            return new MetadataProperty(MetadataTag.Copy(this.Tag));
        }
        public static MetadataProperty Empty() => new(MetadataTag.Empty);
    }
}
