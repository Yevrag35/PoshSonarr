using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Notifications;

namespace MG.Sonarr.Next.Models.Profiles
{
    [SonarrObject]
    public sealed class LanguageProfileObject : IdSonarrObject<LanguageProfileObject>,
        ISerializableNames<LanguageProfileObject>
    {
        const int CAPACITY = 6;
        static readonly string _typeName = typeof(LanguageProfileObject).GetTypeName();

        public string Name
        {
            get => this.GetStringOrEmpty();
            set => this.SetValue(value);
        }
        public LanguageProfileObject()
            : base(CAPACITY)
        {
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            ArgumentNullException.ThrowIfNull(resolver);

            return resolver[Meta.LANGUAGE];
        }

        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
    }
}
