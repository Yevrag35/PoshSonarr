using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Profiles;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles
{
    [Cmdlet(VerbsCommon.Get, "SonarrReleaseProfile")]
    public sealed class GetSonarrReleaseProfileCmdlet : SonarrApiCmdletBase
    {
        SortedSet<int>? _ids;
        HashSet<WildcardString>? _wcNames;
        MetadataTag Tag { get; }

        public GetSonarrReleaseProfileCmdlet()
            : base()
        {
            this.Tag = this.Services.GetRequiredService<MetadataResolver>()[Meta.RELEASE_PROFILE];
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = "ByProfileId")]
        public int[] Id
        {
            get => Array.Empty<int>();
            set
            {
                _ids ??= new();
                _ids.UnionWith(value);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByProfileNameOrId")]
        [SupportsWildcards]
        public IntOrString[] Name
        {
            get => Array.Empty<IntOrString>();
            set
            {
                _ids ??= new();
                _wcNames ??= new();
                value.SplitToSets(_ids, _wcNames,
                    this.MyInvocation.Line.Contains(" -Name ", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        protected override void Process()
        {
            IEnumerable<ReleaseProfileObject> profiles = !_ids.IsNullOrEmpty()
                ? this.GetById(_ids)
                : this.GetByName(_wcNames);

            this.WriteCollection(profiles);
        }

        private IEnumerable<ReleaseProfileObject> GetById(IReadOnlySet<int>? ids)
        {
            if (ids.IsNullOrEmpty())
            {
                yield break;
            }

            foreach (int id in ids)
            {
                string url = this.Tag.GetUrlForId(id);
                var response = this.SendGetRequest<ReleaseProfileObject>(url);
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

        private IEnumerable<ReleaseProfileObject> GetByName(IReadOnlySet<WildcardString>? names)
        {
            var response = this.SendGetRequest<MetadataList<ReleaseProfileObject>>(this.Tag.UrlBase);
            if (response.IsError)
            {
                this.StopCmdlet(response.Error);
                return Enumerable.Empty<ReleaseProfileObject>();
            }
            else if (names.IsNullOrEmpty())
            {
                return response.Data;
            }

            for (int i = response.Data.Count - 1; i >= 0; i--)
            {
                var profile = response.Data[i];
                if (!profile.TryGetNonNullProperty(Constants.NAME, out string? name)
                    ||
                    !names.AnyValueLike(name))
                {
                    response.Data.RemoveAt(i);
                }
            }

            return response.Data;
        }
    }
}
