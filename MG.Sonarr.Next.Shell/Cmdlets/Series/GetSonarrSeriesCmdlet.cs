using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using Namotion.Reflection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsCommon.Get, "SonarrSeries")]
    public sealed class GetSonarrSeriesCmdlet : SonarrApiCmdletBase
    {
        readonly SortedSet<int> _ids;
        readonly SortedSet<WildcardString> _names;

        public GetSonarrSeriesCmdlet()
            : base()
        {
            _ids = new SortedSet<int>();
            _names = new SortedSet<WildcardString>();
        }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "BySeriesName")]
        [SupportsWildcards]
        public IntOrString[] Name
        {
            get => Array.Empty<IntOrString>();
            set => value.SplitToSets(_ids, _names);
        }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesId",
            ValueFromPipelineByPropertyName = true)]
        [Alias("SeriesId")]
        public int[] Id
        {
            get => Array.Empty<int>();
            set => _ids.UnionWith(value);
        }

        protected override void ProcessRecord()
        {
            bool hadIds = false;
            if (_ids.Count > 0)
            {
                hadIds = true;
                foreach (var result in this.GetSeriesById(_ids))
                {
                    this.WriteSonarrResult(result);
                }
            }

            if (_names.Count > 0)
            {
                var response = this.GetSeriesByName(_names);
                this.WriteSonarrResult(response);
            }
            else if (!hadIds)
            {
                var response = this.GetAllSeries();
                this.WriteSonarrResult(response);
            }
        }

        private SonarrResponse<List<object>> GetSeriesByName(IReadOnlySet<WildcardString> names)
        {
            var result = this.GetAllSeries();
            if (result.IsError)
            {
                return result;
            }

            for (int i = result.Data.Count - 1; i >= 0; i--)
            {
                if (!result.Data[i].TryGetProperty("Title", out string? title)
                    ||
                    !names.ValueLike(title))
                {
                    result.Data.RemoveAt(i);
                }
            }

            return result;
        }
        private SonarrResponse<List<object>> GetAllSeries()
        {
            return this.Client.SendGet<List<object>>("/series").GetAwaiter().GetResult();
        }
        private IEnumerable<SonarrResponse<object>> GetSeriesById(IEnumerable<int> ids)
        {
            foreach (int id in ids)
            {
                yield return this.Client.SendGet<object>($"/series/{id}").GetAwaiter().GetResult();
            }
        }
    }
}
