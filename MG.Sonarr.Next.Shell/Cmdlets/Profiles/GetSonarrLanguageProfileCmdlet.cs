using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles
{
    [Cmdlet(VerbsCommon.Get, "SonarrLanguageProfile", DefaultParameterSetName = "ByProfileNameOrId")]
    public sealed class GetSonarrLanguageProfileCmdlet : SonarrApiCmdletBase
    {
        SortedSet<int>? _ids;
        HashSet<WildcardString> _wcNames;

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
            IEnumerable<PSObject> profiles = this.HasParameter(x => x.Id)
                ? this.GetById(_ids)
                : this.GetByName(_wcNames);

            this.WriteCollection(profiles);
        }

        private IEnumerable<PSObject> GetById(IReadOnlySet<int>? ids)
        {
            if (ids.IsNullOrEmpty())
            {
                yield break;
            }

            foreach (int id in ids)
            {
                string url = Constants.LANGUAGE_PROFILE + '/' + id.ToString();
                var response = this.SendGetRequest<PSObject>(url);
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

        private IEnumerable<PSObject> GetByName(IReadOnlySet<WildcardString>? names)
        {
            var response = this.SendGetRequest<List<PSObject>>(Constants.LANGUAGE_PROFILE);
            if (response.IsError)
            {
                this.StopCmdlet(response.Error);
                return Enumerable.Empty<PSObject>();
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
