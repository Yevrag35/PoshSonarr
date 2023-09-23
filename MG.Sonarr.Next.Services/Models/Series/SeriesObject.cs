using MG.Sonarr.Next.Services.Metadata;

namespace MG.Sonarr.Next.Services.Models.Series
{
    public sealed class SeriesObject : SonarrObject
    {
        public SeriesObject()
            : base(46)
        {
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.SERIES];
        }
    }
}
