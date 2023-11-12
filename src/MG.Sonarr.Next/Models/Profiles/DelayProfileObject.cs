using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Profiles
{
    [SonarrObject]
    public sealed class DelayProfileObject : TagUpdateObject<DelayProfileObject>,
        ISerializableNames<DelayProfileObject>
    {
        const int CAPACITY = 10;
        static readonly string _typeName = typeof(DelayProfileObject).GetTypeName();

        public DelayProfileObject()
            : base(CAPACITY)
        {
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.DELAY_PROFILE];
        }

        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }

        static readonly OneStringSet _capitalSet = new("PreferredProtocol");
        static readonly Lazy<JsonNameHolder> _names = new(GetJsonNames);
        private static JsonNameHolder GetJsonNames()
        {
            return JsonNameHolder
                .FromDeserializationNamePairs(new KeyValuePair<string, string>[]
                {
                    new("TorrentDelay", "TorrentDelayInMins"),
                    new("UsenetDelay", "UsenetDelayInMins"),
                });
        }

        public static IReadOnlyDictionary<string, string> GetDeserializedNames()
        {
            return _names.Value.DeserializationNames;
        }
        public static IReadOnlySet<string> GetPropertiesToCapitalize()
        {
            return _capitalSet;
        }
        public static IReadOnlyDictionary<string, string> GetSerializedNames()
        {
            return _names.Value.SerializationNames;
        }
    }
}
