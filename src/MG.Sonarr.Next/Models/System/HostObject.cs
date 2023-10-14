using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.System
{
    [SonarrObject]
    public class HostObject : IdSonarrObject<HostObject>,
        IJsonOnSerializing,
        ISerializableNames<HostObject>
    {
        const int CAPACITY = 32;
        const int CONDITIONAL_CAPACITY = 3;
        private protected Dictionary<string, object?> Conditionals { get; }

        public HostObject()
            : base(CAPACITY)
        {
            this.Conditionals = new(CONDITIONAL_CAPACITY, StringComparer.InvariantCultureIgnoreCase);
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
            this.Properties.Remove(nameof(this.Id));
        }
        public virtual void OnSerializing()
        {
            this.UpdateProperty(x => x.Id);
        }
        public override void Reset()
        {
            base.Reset();
            this.Properties.Remove(nameof(this.Id));
        }
    }

    [SonarrObject]
    public sealed class NoKeyHostObject : HostObject,
        IComparable<NoKeyHostObject>,
        ISerializableNames<NoKeyHostObject>
    {
        static readonly string[] _removeProperties = new[]
        {
            Constants.API_KEY, Constants.PASSWORD, Constants.PROXY_PASSWORD,
        };

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
        public override void Reset()
        {
            base.Reset();
            this.Properties.RemoveMany(_removeProperties);
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

            this.Properties.RemoveMany(_removeProperties);
        }

        public override void OnSerializing()
        {
            base.OnSerializing();
            foreach (var kvp in this.Conditionals)
            {
                this.UpdateProperty(kvp.Key, kvp.Value);
            }
        }
    }
}
