using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Shell.Models;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Services.Models.Series
{
    public sealed class SeriesObject : SonarrObject, IHasId
    {
        public int Id { get; private set; }

        public SeriesObject()
            : base(46)
        {
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.SERIES];
        }

        public override void OnDeserialized()
        {
            if (this.TryGetId(out int id))
            {
                this.Id = id;
            }
        }
    }
}
