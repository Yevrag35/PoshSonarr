using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;

namespace MG.Sonarr.Next.Services.Models.Profiles
{
    public sealed class LanguageProfileObject : SonarrObject
    {
        public int Id { get; private set; }
        public string Name
        {
            get => this.GetValue<string>() ?? string.Empty;
            set => this.SetValue(value);
        }
        public LanguageProfileObject() : base(6)
        {
        }


        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.LANGUAGE];
        }

        public override void OnDeserialized()
        {
            if (this.TryGetId(out int id))
            {
                this.Id = id;
            }
        }
    }
}
