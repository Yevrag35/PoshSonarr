using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.System
{
    [SonarrObject]
    public class HostObject : SonarrObject,
        IHasId,
        IJsonOnSerializing,
        ISerializableNames<HostObject>
    {
        const int CAPACITY = 32;
        const int CONDITIONAL_CAPACITY = 3;
        static readonly string _typeName = typeof(HostObject).GetName();

        private protected Dictionary<string, object?> Conditionals { get; }

        public int Id { get; private set; }

        public HostObject()
            : base(CAPACITY)
        {
            this.Conditionals = new(CONDITIONAL_CAPACITY, StringComparer.OrdinalIgnoreCase);
        }

        public override void Commit()
        {
            base.Commit();
            this.Properties.Remove(nameof(this.Id));
        }
        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.HOST];
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
        public virtual void OnSerializing()
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

        static readonly HashSet<string> _capitalProps = [
            "AuthenticationMethod", "CertificateValidation", "LogLevel", "ProxyType", "UpdateMechanism",
        ];
        public static IReadOnlySet<string> GetPropertiesToCapitalize()
        {
            return _capitalProps;
        }
    }

    [SonarrObject]
    public sealed class NoKeyHostObject : HostObject,
        IComparable<NoKeyHostObject>,
        ISerializableNames<NoKeyHostObject>
    {
        static readonly ImmutableArray<string> _removeProperties =
        [
            Constants.API_KEY, Constants.PASSWORD, Constants.PROXY_PASSWORD,
        ];
        static readonly string _typeName = typeof(NoKeyHostObject).GetName();

        public int CompareTo(NoKeyHostObject? other)
        {
            return base.CompareTo(other);
        }

        public override void Commit()
        {
            base.Commit();
            this.Conditionals.Clear();
            this.StoreConditionals();
        }
        public override void OnDeserialized()
        {
            base.OnDeserialized();
            this.StoreConditionals();
        }
        public override void OnSerializing()
        {
            base.OnSerializing();
            foreach (var kvp in this.Conditionals)
            {
                this.UpdateProperty(kvp.Value, propertyName: kvp.Key);
            }
        }
        public override void Reset()
        {
            base.Reset();
            this.Properties.RemoveMany(_removeProperties.AsSpan());
        }
        private void StoreConditionals()
        {
            foreach (string property in _removeProperties)
            {
                if (this.TryGetProperty(property, out string? propValue))
                {
                    this.Conditionals[property] = propValue ?? string.Empty;
                }
            }

            this.Properties.RemoveMany(_removeProperties.AsSpan());
        }
        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();

            Debug.Assert(this.TypeNames.Count > 0 && this.TypeNames[0] == typeof(HostObject).GetName());
            this.TypeNames[0] = _typeName;
        }
    }
}
