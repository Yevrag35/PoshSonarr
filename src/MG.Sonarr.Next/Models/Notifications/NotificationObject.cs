using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.Notifications
{
    [SonarrObject]
    public class NotificationObject : TagUpdateObject<NotificationObject>,
        ISerializableNames<NotificationObject>
    {
        static readonly string[] _isEnabledProps = new[]
        {
            "OnGrab", "OnDownload", "OnUpgrade", "OnRename", "OnSeriesDelete", "OnEpisodeFileDelete", 
            "OnEpisodeFileDeleteForUpgrade", "OnHealthIssue", "OnApplicationUpdate"
        };
        const int CAPACITY = 38;

        public string Name
        {
            get => this.GetStringOrEmpty();
            set => this.SetValue(value);
        }

        public NotificationObject()
            : base(CAPACITY)
        {
            this.MustUpdateViaApi = false;
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.NOTIFICATION];
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();
            this.MustUpdateViaApi = false;
            bool isEnabled = CalculateIsEnabled(this.Properties);
            this.Properties.Add(new PSNoteProperty("IsEnabled", isEnabled));
        }

        private static bool CalculateIsEnabled<T>(PSMemberInfoCollection<T> properties) where T : PSPropertyInfo
        {
            if (properties.TryGetNonEnumeratedCount(out int count) && count < _isEnabledProps.Length)
            {
                return false;
            }

            foreach (string propName in _isEnabledProps)
            {
                if (!TryGetInfo(properties, propName, out T? info)
                    ||
                    !info.IsGettable
                    ||
                    info.Value is not bool boolVal)
                {
                    return false;
                }
                else if (boolVal)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetInfo<T>(PSMemberInfoCollection<T> properties, string propertyName, [NotNullWhen(true)] out T? result) where T : PSPropertyInfo
        {
            result = properties[propertyName];
            return result is not null;
        }
    }
}
