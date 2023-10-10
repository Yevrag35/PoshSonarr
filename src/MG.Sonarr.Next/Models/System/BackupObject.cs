using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.System
{
    public sealed class BackupObject : SonarrObject,
        IComparable<BackupObject>,
        ISerializableNames<BackupObject>
    {
        const int CAPACITY = 6;

        public Uri BackupUri { get; private set; } = null!;
        public int Id { get; private set; }
        public string Name { get; private set; } = string.Empty;

        public BackupObject()
            : base(CAPACITY)
        {
        }

        public int CompareTo(BackupObject? other)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(this.Name, other?.Name);
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.BACKUP];
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();
            if (this.TryGetId(out int id))
            {
                this.Id = id;
            }

            if (this.TryGetNonNullProperty(nameof(this.Name), out string? name))
            {
                this.Name = name;
            }

            if (this.TryGetNonNullProperty(nameof(this.BackupUri), out string? possible)
                &&
                Uri.TryCreate(possible, UriKind.Relative, out Uri? backupUri))
            {
                this.BackupUri = backupUri;
            }
        }

        const int DICT_CAPACITY = 2;
        public static IReadOnlyDictionary<string, string> GetDeserializedNames()
        {
            return new Dictionary<string, string>(DICT_CAPACITY, StringComparer.InvariantCultureIgnoreCase)
            {
                { "Path", "BackupUri" },
            };
        }
        public static IReadOnlyDictionary<string, string> GetSerializedNames()
        {
            return GetDeserializedNames()
                .ToDictionary(x => x.Value, x => x.Key, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
