using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Services.Auth;

namespace MG.Sonarr.Next.Models.System
{
    [SonarrObject]
    public sealed class SystemStatusObject : SonarrObject,
        ISerializableNames<SystemStatusObject>
    {
        const int CAPACITY = 26;
        static readonly string _typeName = typeof(SystemStatusObject).GetTypeName();

        public SonarrAuthType Authentication { get; private set; }

        public SystemStatusObject()
            : base(CAPACITY)
        {
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.STATUS];
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();
            if (this.TryGetProperty(nameof(this.Authentication), out string? at) && Enum.TryParse(at, true, out SonarrAuthType authType))
            {
                this.Authentication = authType;
            }
        }
        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }

        static readonly HashSet<string> _capitalProps = new(4, StringComparer.InvariantCultureIgnoreCase)
        {
            "Authentication", "Mode", "PackageUpdateMechanism", "RuntimeName"
        };
        public static IReadOnlySet<string> GetPropertiesToCapitalize()
        {
            return _capitalProps;
        }
    }
}

