using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Services.Models.Profiles;
using MG.Sonarr.Next.Services.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles
{
    [Cmdlet(VerbsCommon.Get, "SonarrDelayProfile")]
    public sealed class GetSonarrDelayProfileCmdlet : SonarrApiCmdletBase
    {
        SortedSet<int>? _ids;
        MetadataTag Tag { get; }

        public GetSonarrDelayProfileCmdlet()
            : base()
        {
            this.Tag = this.Services.GetRequiredService<MetadataResolver>()[Meta.DELAY_PROFILE];
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false)]
        public int[] Id
        {
            get => Array.Empty<int>();
            set
            {
                _ids ??= new();
                _ids.UnionWith(value);
            }
        }

        protected override void Process()
        {
            IEnumerable<PSObject> profiles = !_ids.IsNullOrEmpty()
                ? this.GetById(_ids)
                : this.GetByName();

            this.WriteCollection(profiles);
        }

        private IEnumerable<DelayProfileObject> GetById(IReadOnlySet<int>? ids)
        {
            if (ids.IsNullOrEmpty())
            {
                yield break;
            }

            foreach (int id in ids)
            {
                string url = this.Tag.GetUrlForId(id);
                var response = this.SendGetRequest<DelayProfileObject>(url);
                if (response.IsError)
                {
                    this.WriteConditionalError(response.Error);
                }
                else
                {
                    yield return response.Data;
                }
            }
        }

        private IEnumerable<DelayProfileObject> GetByName()
        {
            var response = this.SendGetRequest<MetadataList<DelayProfileObject>>(this.Tag.UrlBase);
            if (response.IsError)
            {
                this.StopCmdlet(response.Error);
                return Enumerable.Empty<DelayProfileObject>();
            }

            return response.Data;
        }
    }
}
