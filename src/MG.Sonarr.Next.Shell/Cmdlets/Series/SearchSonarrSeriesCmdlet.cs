using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using System.Net;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsCommon.Search, "SonarrSeries", DefaultParameterSetName = "BySeriesName")]
    public sealed class SearchSonarrSeriesCmdlet : SonarrApiCmdletBase
    {
        static readonly string SEARCH_STR_QUERY = Constants.SERIES + "/lookup?term={0}";
        static readonly string SEARCH_ID_QUERY = Constants.SERIES + "/lookup?term=tvdb:{0}";
        Wildcard _wildcardStr;
        MetadataTag _tag = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesName")]
        public string Name
        {
            get => string.Empty;
            set => _wildcardStr = value;
        }

        [Parameter(Mandatory = true, ParameterSetName = "ByTVDbId")]
        public long TVDbId { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = "BySeriesName")]
        public SwitchParameter Strict { get; set; }

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _tag = provider.GetRequiredService<IMetadataResolver>()[Meta.SERIES_ADD];
        }
        protected override void Process(IServiceProvider provider)
        {
            string path = this.HasParameter(x => x.Name)
                ? GetSearchByNamePath(_wildcardStr)
                : string.Format(SEARCH_ID_QUERY, this.TVDbId);

            var result = this.SendGetRequest<List<AddSeriesObject>>(path);
            if (result.IsError)
            {
                this.StopCmdlet(result.Error);
                return;
            }

            if (this.Strict)
            {
                this.ProcessStricly(result.Data, in _wildcardStr);
            }
            else
            {
                this.WriteCollection(result.Data);
            }
        }

        private void ProcessStricly(IEnumerable<AddSeriesObject> values, in Wildcard Wildcard)
        {
            foreach (AddSeriesObject pso in values)
            {
                if (StrictlyMatches(pso.Title, in Wildcard))
                {
                    this.WriteObject(pso);
                }
            }
        }

        private static string GetSearchByNamePath(in Wildcard name)
        {
            string use = name.ContainsWildcards
                ? ((string)name).Replace('*', ' ').Replace('?', ' ').Trim()
                : (string)name;

            return string.Format(SEARCH_STR_QUERY, WebUtility.UrlEncode(use));
        }

        private static bool StrictlyMatches(string value, in Wildcard stringToMatch)
        {
            return stringToMatch.IsMatch(value)
                   ||
                   value.Contains((string)stringToMatch, StringComparison.OrdinalIgnoreCase);
        }
    }
}
