using MG.Sonarr.Next.Attributes;
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

        public void OnSerializing()
        {
            this.Properties.Remove(nameof(this.Id));
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();
            this.Properties.Remove(nameof(this.Id));
        }
    }
}
