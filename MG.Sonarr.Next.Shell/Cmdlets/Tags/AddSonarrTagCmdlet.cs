using MG.Sonarr.Next.Services.Metadata;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags
{
    [Cmdlet(VerbsCommon.Add, "SonarrTag")]
    public sealed class AddSonarrTagCmdlet : SonarrApiCmdletBase
    {
        MetadataResolver Resolver { get; }
        readonly HashSet<string> _urls;

        public AddSonarrTagCmdlet()
            : base()
        {
            _urls = new(1, StringComparer.InvariantCultureIgnoreCase);
            this.Resolver = this.Services.GetRequiredService<MetadataResolver>();
        }


        private void AddUrlsFromMetadata(object[]? array)
        {
            if (array is null)
            {
                return;
            }

            foreach (PSObject pso in array.OfType<PSObject>())
            {
                if (this.Resolver.TryGetValue(pso, out MetadataTag? tag) && tag.SupportsId)
                {

                }
            }
        }
    }
}
