using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Services.Models.Qualities
{
    public sealed class QualityDefinitionObject : SonarrObject
    {
        public int Id { get; private set; }
        public string Title
        {
            get => this.GetValue<string>() ?? string.Empty;
            set => this.SetValue(value);
        }

        public QualityDefinitionObject()
            : base(10)
        {
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.QUALITY_DEFINITION];
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
