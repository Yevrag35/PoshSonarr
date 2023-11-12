using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.DownloadClients;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Media
{
    [SonarrObject]
    public sealed class MediaManagementObject : SonarrObject,
        IComparable<MediaManagementObject>,
        IJsonOnSerializing,
        ISerializableNames<MediaManagementObject>
    {
        static readonly PSAliasProperty _pathAlias = new("Path", "RecycleBinPath");
        static readonly string _typeName = typeof(MediaManagementObject).GetTypeName();
        const int CAPACITY = 21;

        public int Id { get; private set; }
        public string RecycleBinPath
        {
            get => this.GetStringOrEmpty();
            set => this.SetValue(value ?? string.Empty);
        }

        public MediaManagementObject()
            : base(CAPACITY)
        {
        }

        public override void Commit()
        {
            this.Reset();
        }
        public int CompareTo(MediaManagementObject? other)
        {
            return Comparer<int?>.Default.Compare(this.Id, other?.Id);
        }
        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.MEDIA_MANGEMENT];
        }
        public bool IsRecycleBinPathProper(out string path)
        {
            path = this.RecycleBinPath;
            return string.IsNullOrWhiteSpace(path) || path.EndsWith('\\');
        }
        public override void OnDeserialized()
        {
            base.OnDeserialized();
            if (this.TryGetId(out int id))
            {
                this.Id = id;
            }

            if (this.TryGetNonNullProperty(_pathAlias.ReferencedMemberName, out string? path))
            {
                this.RecycleBinPath = path;
            }

            this.Reset();
        }
        public void OnSerializing()
        {
            this.Properties.Remove(_pathAlias.Name);
            this.UpdateProperty(x => x.Id);
        }
        public override void Reset()
        {
            base.Reset();
            this.Properties.Remove(Constants.ID);
            this.Properties.Add(_pathAlias);
        }
        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }

        static readonly Lazy<JsonNameHolder> _names = new(() => JsonNameHolder
            .FromSingleNamePair("RecycleBin", "RecycleBinPath"));

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
