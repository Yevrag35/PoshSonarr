using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Episodes;
using MG.Sonarr.Next.Services.Models.Series;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Episodes
{
    [Cmdlet(VerbsCommon.Get, "SonarrEpisode", DefaultParameterSetName = "BySeriesInput")]
    public sealed class GetSonarrEpisodeCmdlet : SonarrApiCmdletBase
    {
        const string SERIES_ID = "seriesId";

        SortedSet<int> EpIds { get; set; } = null!;
        SortedSet<int> SeriesIds { get; set; } = null!;
        MetadataTag Tag { get; }

        public GetSonarrEpisodeCmdlet()
            : base()
        {
            this.Tag = this.Services.GetRequiredService<MetadataResolver>()[Meta.EPISODE];
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "BySeriesInput")]
        public SeriesObject[] InputObject
        {
            get => Array.Empty<SeriesObject>();
            set
            {
                this.SeriesIds ??= new();
                this.SeriesIds.UnionWith(value.Select(x => x.Id));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByEpisodeId")]
        public int[] Id
        {
            get => Array.Empty<int>();
            set
            {
                this.EpIds ??= new();
                this.EpIds.UnionWith(value);
            }
        }

        protected override ErrorRecord? End()
        {
            IEnumerable<EpisodeObject> episodes = this.HasParameter(x => x.Id)
                ? this.GetEpisodesById<EpisodeObject>(this.EpIds)
                : this.GetEpisodesBySeries<EpisodeObject>(this.SeriesIds);

            this.WriteCollection(episodes);

            return null;
        }

        private IEnumerable<T> GetEpisodesById<T>(IReadOnlySet<int> episodeIds) where T : PSObject, IJsonMetadataTaggable
        {
            foreach (int id in episodeIds)
            {
                string url = this.Tag.GetUrlForId(id);
                var response = this.SendGetRequest<T>(url);
                if (response.IsError)
                {
                    this.WriteError(response.Error);
                }
                else
                {
                    yield return response.Data;
                }
            }
        }

        private IEnumerable<T> GetEpisodesBySeries<T>(IReadOnlySet<int> seriesId) where T : PSObject, IJsonMetadataTaggable
        {
            QueryParameterCollection queryCol = new();
            foreach (int id in seriesId)
            {
                queryCol.Add(SERIES_ID, id);
                string url = this.Tag.GetUrl(queryCol);
                var response = this.SendGetRequest<MetadataList<T>>(url);
                if (response.IsError)
                {
                    this.WriteError(response.Error);
                    continue;
                }

                foreach (T obj in response.Data)
                {
                    yield return obj;
                }
            }
        }
    }
}
