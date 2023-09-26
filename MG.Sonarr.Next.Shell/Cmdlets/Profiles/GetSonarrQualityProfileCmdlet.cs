using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Qualities;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Shell.Cmdlets.Qualities
{
    [Cmdlet(VerbsCommon.Get, "SonarrQualityProfile", DefaultParameterSetName = "ByProfileName")]
    [Alias("Get-SonarrProfile")]
    public sealed class GetSonarrQualityProfileCmdlet : SonarrApiCmdletBase
    {
        SortedSet<int>? _ids;
        HashSet<WildcardString>? _wcNames;
        MetadataTag Tag { get; }

        public GetSonarrQualityProfileCmdlet()
            : base()
        {
            this.Tag = this.Services.GetRequiredService<MetadataResolver>()[Meta.QUALITY_PROFILE];
            _ids = new();
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = "ByPipelineInput", ValueFromPipeline = true)]
        public IQualityProfilePipeable[] InputObject
        {
            get => Array.Empty<IQualityProfilePipeable>();
            set
            {
                _ids ??= new();
                _ids.UnionWith(value.Where(x => x.QualityProfileId > 0).Select(x => x.QualityProfileId));
            }
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
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByProfileName")]
        [SupportsWildcards]
        public IntOrString[] Name
        {
            get => Array.Empty<IntOrString>();
            set
            {
                _wcNames ??= new(value.Length);
                _ids ??= new();

                value.SplitToSets(_ids, _wcNames,
                    this.MyInvocation.Line.Contains(" -Name ", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        protected override void End()
        {
            if (this.InvokeCommand.HasErrors)
            {
                return;
            }

            IEnumerable<QualityProfileObject> profiles = !_ids.IsNullOrEmpty()
                ? this.GetProfileById(_ids)
                : this.GetProfileByName(_wcNames);

            this.WriteCollection(profiles);
        }

        private IEnumerable<QualityProfileObject> GetProfileById(IReadOnlySet<int>? ids)
        {
            if (ids is null)
            {
                yield break;
            }

            foreach (int id in ids)
            {
                string url = this.Tag.GetUrlForId(id);
                var response = this.SendGetRequest<QualityProfileObject>(url);
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
        private IEnumerable<QualityProfileObject> GetProfileByName(IReadOnlySet<WildcardString>? names)
        {
            var all = this.SendGetRequest<MetadataList<QualityProfileObject>>(this.Tag.UrlBase);
            if (all.IsError)
            {
                this.StopCmdlet(all.Error);
                return Enumerable.Empty<QualityProfileObject>();
            }
            else if (names is null)
            {
                return all.Data;
            }

            for (int i = all.Data.Count - 1; i >= 0; i--)
            {
                if (!names.AnyValueLike(all.Data[i].Name))
                {
                    all.Data.RemoveAt(i);
                }
            }

            return all.Data;
        }
    }
}
