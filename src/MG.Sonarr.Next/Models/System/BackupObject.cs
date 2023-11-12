using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.System
{
    [SonarrObject]
    public sealed class BackupObject : IdSonarrObject<BackupObject>,
        ISerializableNames<BackupObject>
    {
        const int CAPACITY = 6;
        static readonly string _typeName = typeof(BackupObject).GetTypeName();

        public Uri BackupUri { get; private set; } = null!;
        public string Name { get; private set; } = string.Empty;

        public BackupObject()
            : base(CAPACITY)
        {
        }

        public override int CompareTo(BackupObject? other)
        {
            int compare = StringComparer.InvariantCultureIgnoreCase.Compare(this.Name, other?.Name);
            if (compare == 0)
            {
                compare = base.CompareTo(other);
            }

            return compare;
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.BACKUP];
        }

        protected override void OnDeserialized(bool alreadyCalled)
        {
            base.OnDeserialized(alreadyCalled);

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
        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }

        const int DICT_CAPACITY = 1;
        private static readonly Lazy<JsonNameHolder> _names = new(GetJsonNames);

        private static JsonNameHolder GetJsonNames()
        {
            return JsonNameHolder.FromSingleNamePair("Path", "BackupUri");
        }

        public static IReadOnlyDictionary<string, string> GetDeserializedNames()
        {
            return _names.Value.DeserializationNames;
        }
        public static IReadOnlyDictionary<string, string> GetSerializedNames()
        {
            return _names.Value.SerializationNames;
        }
    }
}
