using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Notifications
{
    [SonarrObject]
    public sealed class NotificationSchemaObject : TagUpdateObject<NotificationSchemaObject>,
        IJsonOnSerializing,
        ISchemaObject,
        ISerializableNames<NotificationSchemaObject>
    {
        const int CAPACITY = 38;
        static readonly string _typeName = typeof(NotificationSchemaObject).GetTypeName();
        public override int Id => -1;
        public bool IsTaggable => true;

        public NotificationSchemaObject()
            : base(CAPACITY)
        {
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.NOTIFICATION];
        }
        public override void OnDeserialized()
        {
            base.OnDeserialized();
            this.Properties.Remove(nameof(this.Id));
        }
        public void OnSerializing()
        {
            this.Properties.Remove(nameof(this.Id));
        }
        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
    }
}
