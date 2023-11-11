using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.RemotePaths
{
    [SonarrObject]
    public sealed class RemotePathObject : IdSonarrObject<RemotePathObject>,
        ISerializableNames<RemotePathObject>
    {
        const int CAPACITY = 5;
        static readonly string _typeName = typeof(RemotePathObject).GetTypeName();

        public string HostName { get; private set; } = string.Empty;
        public string LocalPath { get; private set; } = string.Empty;
        public string RemotePath { get; private set; } = string.Empty;

        public RemotePathObject() : base(CAPACITY)
        {
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.REMOTE_PATH_MAPPING];
        }

        public void ApplyFromPost(RemotePathBody body)
        {
            if (!body.Id.HasValue && this.Id != body.Id)
            {
                throw new ArgumentException("The body must represent an existing remote path and must match this object.");
            }

            this.HostName = body.Host;
            this.LocalPath = body.LocalPath;
            this.RemotePath = body.RemotePath;

            this.Commit();
        }

        public override void Commit()
        {
            base.Commit();
            this.UpdateProperty(x => x.HostName);
            this.UpdateProperty(x => x.LocalPath);
            this.UpdateProperty(x => x.RemotePath);
        }

        protected override void OnDeserialized(bool alreadyCalled)
        {
            if (this.TryGetNonNullProperty(nameof(this.HostName), out string? host))
            {
                this.HostName = host;
            }

            if (this.TryGetNonNullProperty(nameof(this.LocalPath), out string? localPath))
            {
                this.LocalPath = localPath;
            }

            if (this.TryGetNonNullProperty(nameof(this.RemotePath), out string? remotePath))
            {
                this.RemotePath = remotePath;
            }
        }

        public override void Reset()
        {
            base.Reset();
            this.UpdateProperty(x => x.HostName);
            this.UpdateProperty(x => x.LocalPath);
            this.UpdateProperty(x => x.RemotePath);
        }

        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }

        private static readonly Lazy<JsonNameHolder> _names = new(GetJsonNames);
        private static JsonNameHolder GetJsonNames()
        {
            return JsonNameHolder.FromSingleNamePair("Host", "HostName");
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

