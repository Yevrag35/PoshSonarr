﻿using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Series;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsCommon.Search, "SonarrSeries", DefaultParameterSetName = "BySeriesName")]
    public sealed class SearchSonarrSeriesCmdlet : SonarrApiCmdletBase
    {
        const string SEARCH_STR_QUERY = Constants.SERIES + "/lookup?term={0}";
        const string SEARCH_ID_QUERY = Constants.SERIES + "/lookup?term=tvdb:{0}";
        WildcardString _wildcardStr;
        MetadataTag Tag { get; set; } = null!;

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

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            this.Tag = provider.GetRequiredService<MetadataResolver>()[Meta.SERIES_ADD];
        }
        protected override void Process(IServiceProvider provider)
        {
            string path = this.HasParameter(x => x.Name)
                ? GetSearchByNamePath(this.Name)
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

        private void ProcessStricly(IEnumerable<AddSeriesObject> values, in WildcardString wildcardString)
        {
            foreach (AddSeriesObject pso in values)
            {
                if (StrictlyMatches(pso.Title, in wildcardString))
                {
                    this.WriteObject(pso);
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
