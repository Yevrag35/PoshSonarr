using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Errors
{
    [SonarrObject]
    public sealed class SonarrServerError : SonarrObject,
        IComparable<SonarrServerError>,
        ISerializableNames<SonarrServerError>
    {
        const int CAPACITY = 5;
        const string STRING_TYPE = "System.String";
        static readonly string _typeName = typeof(SonarrServerError).GetTypeName();

        public string Message { get; private set; } = string.Empty;
        public string? PropertyName { get; private set; }

        public SonarrServerError()
            : base(CAPACITY)
        {
        }

        public int CompareTo(SonarrServerError? other)
        {
            int compare = StringComparer.InvariantCultureIgnoreCase.Compare(this.PropertyName, other?.PropertyName);
            if (compare == 0)
            {
                compare = StringComparer.InvariantCultureIgnoreCase.Compare(this.Message, other?.Message);
            }

            return compare;
        }
        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return existing;
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();
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

        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
    }
}
