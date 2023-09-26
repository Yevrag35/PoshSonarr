using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Shell.Cmdlets.Qualities
{
    [Cmdlet(VerbsCommon.Get, "SonarrQualityDefinition")]
    [Alias("Get-SonarrQuality")]
    public sealed class GetSonarrQualityDefinitionCmdlet : SonarrApiCmdletBase
    {
        HashSet<int> _ids = null!;
        HashSet<WildcardString> _wcNames = null!;
        //bool _explicitlySetName;

        const string NAME = " -Name ";
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0)]
        [SupportsWildcards]
        public IntOrString[] Name
        {
            get => Array.Empty<IntOrString>();
            set
            {
                _ids ??= new();
                _wcNames ??= new();
                value.SplitToSets(_ids, _wcNames,
                    this.MyInvocation.Line.Contains(NAME, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false)]
        public int[] Id
        {
            get => Array.Empty<int>();
            set
            {
                _ids ??= new(1);
                _ids.UnionWith(value.Where(x => x > 0));
            }
        }

        //[DebuggerBrowsable(DebuggerBrowsableState.Never)]
        //[Parameter(Mandatory = true, DontShow = true, ValueFromPipeline = true)]
        //public IQualityDefinitionPipeable[] InputObject
        //{
        //    get => Array.Empty<IQualityDefinitionPipeable>();
        //    set
        //    {
        //        _ids ??= new(value.Length);
        //        _ids.UnionWith(value.Where(x => x.QualityId > 0).Select(x => x.QualityId));
        //    }
        //}

        protected override void Process()
        {
            List<PSObject> list = new(17);

            bool addedIds = false;
            if (_ids is not null && _ids.Count > 0)
            {
                list.AddRange(this.GetById(_ids));
                addedIds = true;
            }

            list.AddRange(this.GetByName(_wcNames, addedIds));

            foreach (PSObject obj in list)
            {
                this.WriteObject(obj);
            }
        }

        private IEnumerable<PSObject> GetById(IReadOnlySet<int>? ids)
        {
            if (ids is null)
            {
                yield break;
            }

            foreach (int id in ids)
            {
                string url = Constants.QUALITY_DEFINITIONS + '/' + id.ToString();
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

        private IEnumerable<PSObject> GetByName(IReadOnlySet<WildcardString>? names, bool addedIds)
        {
            bool empty = names.IsNullOrEmpty();
            if (empty && addedIds)
            {
                return Enumerable.Empty<PSObject>();
            }

            var allQualities = this.SendGetRequest<List<PSObject>>(Constants.QUALITY_DEFINITIONS);
            if (allQualities.IsError)
            {
                this.StopCmdlet(allQualities.Error);
                return Enumerable.Empty<PSObject>();
            }
            else if (empty)
            {
                return allQualities.Data;
            }

            for (int i = allQualities.Data.Count - 1; i >= 0; i--)
            {
                var pso = allQualities.Data[i];
                if (!pso.TryGetNonNullProperty(Constants.TITLE, out string? title)
                    ||
                    !names!.AnyValueLike(title))
                {
                    allQualities.Data.RemoveAt(i);
                }
            }

            return allQualities.Data;
        }
    }
}
