using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Episodes;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Episodes
{
    [Cmdlet(VerbsCommon.Get, "SonarrEpisodeFile")]
    public sealed class GetSonarrEpisodeFileCmdlet : SonarrApiCmdletBase
    {
        bool _disposed;
        SortedSet<int> Ids { get; set; } = null!;
        SortedSet<int> SeriesIds { get; set; } = null!;
        MetadataTag Tag { get; set; } = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByEpisodeFileInput", DontShow = true)]
        public IEpisodeFilePipeable[] InputObject { get; set; } = Array.Empty<IEpisodeFilePipeable>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "BySeriesInput", DontShow = true)]
        public IEpisodeFileBySeriesPipeable[] SeriesInput { get; set; } = Array.Empty<IEpisodeFileBySeriesPipeable>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByEpisodeFileId")]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = Array.Empty<int>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = "BySeriesId")]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] SeriesId { get; set;  } = Array.Empty<int>();

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            this.Tag = provider.GetRequiredService<MetadataResolver>()[Meta.EPISODE_FILE];
            var pool = provider.GetRequiredService<IObjectPool<SortedSet<int>>>();
            this.Ids = pool.Get();
            this.SeriesIds = pool.Get();
        }

        private bool HasNoParameters()
        {
            return this.Ids.Count <= 0
                   &&
                   this.SeriesIds.Count <= 0;
        }

        protected override void Begin(IServiceProvider provider)
        {
            this.Ids.UnionWith(this.Id);
            this.SeriesIds.UnionWith(this.SeriesId);
        }
        protected override void Process(IServiceProvider provider)
        {
            if (this.HasParameter(x => x.InputObject))
            {
                this.Ids.UnionWith(
                    this.InputObject
                        .Where(x => x.EpisodeFileId > 0)
                            .Select(x => x.EpisodeFileId));
            }
            else if (this.HasParameter(x => x.SeriesInput))
            {
                this.SeriesIds.UnionWith(
                    this.SeriesInput
                        .Where(x => x.SeriesId > 0)
                            .Select(x => x.SeriesId));
            }
        }
        protected override void End(IServiceProvider provider)
        {
            if (this.InvokeCommand.HasErrors || this.HasNoParameters())
            {
                return;
            }

            IEnumerable<EpisodeFileObject> files = ParameterNameStartsWithSeries(this.ParameterSetName)
                ? this.GetEpFilesBySeriesId(this.SeriesIds)
                : this.GetEpFilesById(this.Ids);

            this.WriteCollection(files);
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

        protected override void Dispose(bool disposing, IServiceScopeFactory? factory)
        {
            if (disposing && !_disposed)
            {
                if (factory is not null)
                {
                    using var scope = factory.CreateScope();
                    var pool = scope.ServiceProvider.GetService<IObjectPool<SortedSet<int>>>();
                    pool?.Return(this.Ids);
                    pool?.Return(this.SeriesIds);
                }
                
                this.Ids = null!;
                this.SeriesIds = null!;
                _disposed = true;
            }

            base.Dispose(disposing, factory);
        }
    }
}
