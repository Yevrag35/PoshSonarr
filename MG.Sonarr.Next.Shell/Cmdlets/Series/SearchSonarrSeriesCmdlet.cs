using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsCommon.Search, "SonarrSeries", DefaultParameterSetName = "BySeriesName")]
    public sealed class SearchSonarrSeriesCmdlet : SonarrApiCmdletBase
    {
        const string SEARCH_STR_QUERY = Constants.SERIES + "/lookup?term={0}";
        const string SEARCH_ID_QUERY = Constants.SERIES + "/lookup?term=tvdb:{0}";
        WildcardString _wildcardStr;
        MetadataTag Tag { get; }

        public SearchSonarrSeriesCmdlet()
            : base()
        {
            this.Tag = this.Services.GetRequiredService<MetadataResolver>()[Meta.SERIES_ADD];
        }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesName")]
        public string Name
        {
            get => _wildcardStr.Value;
            set => _wildcardStr = value;
        }

        [Parameter(Mandatory = true, ParameterSetName = "ByTVDbId")]
        public long TVDbId { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = "BySeriesName")]
        public SwitchParameter Strict { get; set; }

        protected override ErrorRecord? Process()
        {
            string path = this.HasParameter(x => x.Name)
                ? GetSearchByNamePath(this.Name)
                : string.Format(SEARCH_ID_QUERY, this.TVDbId);

            var result = this.SendGetRequest<List<PSObject>>(path);
            if (result.IsError)
            {
                return result.Error;
            }

            if (this.Strict)
            {
                this.ProcessStricly(result.Data, in _wildcardStr);
            }
            else
            {
                this.WriteCollection(this.Tag, result.Data);
            }

            return null;
        }

        private void ProcessStricly(IEnumerable<PSObject> values, in WildcardString wildcardString)
        {
            foreach (PSObject pso in values)
            {
                if (pso.TryGetNonNullProperty(Constants.TITLE, out string? title)
                    &&
                    StrictlyMatches(title, in wildcardString))
                {
                    this.WriteObject(this.Tag, pso);
                }
            }
        }

        private static string GetSearchByNamePath(string name)
        {
            return string.Format(SEARCH_STR_QUERY, WebUtility.UrlEncode(name));
        }

        private static bool StrictlyMatches(string value, in WildcardString stringToMatch)
        {
            return stringToMatch.IsMatch(value)
                   ||
                   value.Contains(stringToMatch.Value, StringComparison.OrdinalIgnoreCase);
        }
    }
}
