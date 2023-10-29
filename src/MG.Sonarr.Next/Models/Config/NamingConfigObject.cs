using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Config
{
    [SonarrObject]
    public sealed class NamingConfigObject : SonarrObject,
        IHasId,
        IJsonOnSerializing,
        ISerializableNames<NamingConfigObject>
    {
        const int CAPACITY = 16;
        static readonly string _typeName = typeof(NamingConfigObject).GetTypeName();

        public int Id { get; private set; }

        public NamingConfigObject()
            : base(CAPACITY)
        {
        }

        public override void Commit()
        {
            base.Commit();
            this.Properties.Remove(nameof(this.Id));
        }
        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.NAMING_CONFIG];
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();
            if (this.TryGetId(out int id))
            {
                this.Id = id;
            }

            this.Properties.Remove(nameof(this.Id));
        }
        public void OnSerializing()
        {
            this.ReplaceNumberProperty(nameof(this.Id), this.Id);
        }
        public override void Reset()
        {
            base.Reset();
            this.Properties.Remove(nameof(this.Id));
        }
        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }

    }
}

