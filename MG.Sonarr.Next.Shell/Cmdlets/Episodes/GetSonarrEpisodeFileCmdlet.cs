using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Episodes;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Shell.Cmdlets.Episodes
{
    [Cmdlet(VerbsCommon.Get, "SonarrEpisodeFile")]
    public sealed class GetSonarrEpisodeFileCmdlet : SonarrApiCmdletBase
    {
        SortedSet<int>? Ids { get; set; }
        SortedSet<int>? SeriesIds { get; set; }
        MetadataTag Tag { get; }

        public GetSonarrEpisodeFileCmdlet()
            : base()
        {
            this.Tag = this.Services.GetRequiredService<MetadataResolver>()[Meta.EPISODE_FILE];
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByEpisodeFileInput", DontShow = true)]
        public IEpisodeFilePipeable[] InputObject
        {
            get => Array.Empty<IEpisodeFilePipeable>();
            set
            {
                this.Ids ??= new();
                this.Ids.UnionWith(
                    value.Where(x => x.EpisodeFileId > 0).Select(x => x.EpisodeFileId));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "BySeriesInput", DontShow = true)]
        public IEpisodeFileBySeriesPipeable[] SeriesInput
        {
            get => Array.Empty<IEpisodeFileBySeriesPipeable>();
            set
            {
                this.SeriesIds ??= new();
                this.SeriesIds.UnionWith(value.Select(x => x.SeriesId));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByEpisodeFileId")]
        public int[] Id
        {
            get => Array.Empty<int>();
            set
            {
                this.Ids ??= new();
                this.Ids.UnionWith(value);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = "BySeriesId")]
        public int[] SeriesId
        {
            get => Array.Empty<int>();
            set
            {
                this.SeriesIds ??= new();
                this.SeriesIds.UnionWith(value);
            }
        }

        private bool HasNoParameters()
        {
            return (this.Ids?.Count).GetValueOrDefault() <= 0
                   &&
                   (this.SeriesIds?.Count).GetValueOrDefault() <= 0;
        }

        protected override void End()
        {
            if (this.InvokeCommand.HasErrors || this.HasNoParameters())
            {
                return;
            }

            IEnumerable<EpisodeFileObject> files = ParameterNameStartsWithSeries(this.ParameterSetName)
                ? this.GetEpFilesBySeriesId(this.SeriesIds)
                : this.GetEpFilesById(this.Ids);

            foreach (var item in files)
            {
                this.WriteObject(item);
            }
        }

        private IEnumerable<EpisodeFileObject> GetEpFilesById(IReadOnlySet<int>? fileIds)
        {
            if (fileIds is null)
            {
                yield break;
            }

            foreach (int id in fileIds)
            {
                string url = this.Tag.GetUrlForId(id);
                var response = this.SendGetRequest<EpisodeFileObject>(url);
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
        private IEnumerable<EpisodeFileObject> GetEpFilesBySeriesId(IReadOnlySet<int>? seriesIds)
        {
            if (seriesIds is null)
            {
                yield break;
            }

            QueryParameterCollection queryCol = new();
            foreach (int id in seriesIds)
            {
                queryCol.Add(Constants.SERIES_ID, id);
                string url = this.Tag.GetUrl(queryCol);
                var response = this.SendGetRequest<MetadataList<EpisodeFileObject>>(url);
                if (response.IsError)
                {
                    this.WriteConditionalError(response.Error);
                    continue;
                }

                foreach (var item in response.Data)
                {
                    yield return item;
                }

                queryCol.Clear();
            }
        }
        private static bool ParameterNameStartsWithSeries(ReadOnlySpan<char> setName)
        {
            return setName.StartsWith(
                stackalloc char[] { 'b', 'y', 's', 'e', 'r', 'i', 'e', 's' }, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
