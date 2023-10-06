using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;

namespace MG.Sonarr.Next.Services.Models
{
    public sealed class SonarrServerError : SonarrObject
    {
        const string STRING_TYPE = "System.String";

        public string Message { get; private set; } = string.Empty;
        public string? PropertyName { get; private set; }

        public SonarrServerError()
            : base(5)
        {
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return existing;
        }

        public override void OnDeserialized()
        {
            var col = this.Properties.Match("*message");
            if (col.Count > 0)
            {
                var values = col
                    .Where(x => x.IsGettable && x.TypeNameOfValue == STRING_TYPE)
                    .Select(x => x.Value.ToString());

                this.Message = string.Join(Environment.NewLine, values);
            }

            if (this.TryGetNonNullProperty(nameof(this.PropertyName), out string? propName))
            {
                this.PropertyName = propName;
            }
        }
    }
}
