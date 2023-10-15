using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Profiles
{
    [SonarrObject]
    public sealed class LanguageProfileObject : IdSonarrObject<LanguageProfileObject>,
        ISerializableNames<LanguageProfileObject>
    {
        const int CAPACITY = 6;

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
    }
}
