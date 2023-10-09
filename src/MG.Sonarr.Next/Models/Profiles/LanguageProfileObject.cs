using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Profiles
{
    public sealed class LanguageProfileObject : SonarrObject,
        IComparable<LanguageProfileObject>,
        ISerializableNames<LanguageProfileObject>
    {
        const int CAPACITY = 6;

        public int Id { get; private set; }
        public string Name
        {
            get => this.GetValue<string>() ?? string.Empty;
            set => this.SetValue(value);
        }
        public LanguageProfileObject()
            : base(CAPACITY)
        {
        }

        public int CompareTo(LanguageProfileObject? other)
        {
            return Comparer<int?>.Default.Compare(this.Id, other?.Id);
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            ArgumentNullException.ThrowIfNull(resolver);

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
