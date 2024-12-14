using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections.Pools;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Episodes
{
    [Cmdlet(VerbsCommon.Get, "SonarrEpisodeFile")]
    [MetadataCanPipe(Tag = Meta.CALENDAR)]
    [MetadataCanPipe(Tag = Meta.EPISODE)]
    public sealed class GetSonarrEpisodeFileCmdlet : SonarrMetadataCmdlet
    {
        bool _disposed;
        const int CAPACITY = 3;
        SortedSet<int> _ids = null!;
        SortedSet<int> _seriesIds = null!;
        QueryCol _params = null!;

        protected override int Capacity => CAPACITY;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByEpisodeFileInput")]
        [ValidateIds(ValidateRangeKind.Positive, typeof(IEpisodeFilePipeable))]
        public IEpisodeFilePipeable[] InputObject { get; set; } = Array.Empty<IEpisodeFilePipeable>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "BySeriesInput")]
        [ValidateIds(ValidateRangeKind.Positive, typeof(IEpisodeFileBySeriesPipeable))]
        public IEpisodeFileBySeriesPipeable[] SeriesInput { get; set; } = Array.Empty<IEpisodeFileBySeriesPipeable>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByEpisodeFileId")]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = [];

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = "BySeriesId")]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] SeriesId { get; set; } = Array.Empty<int>();

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.EPISODE_FILE];
        }
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            var pool = provider.GetRequiredService<IObjectPool<SortedSet<int>>>();
            _ids = pool.Get();
            _seriesIds = pool.Get();
            _params = this.GetPooledObject<QueryCol>();

            this.SetReturnables(_ids, _seriesIds, _params);
        }

        private bool HasNoParameters()
        {
            return _ids.Count <= 0
                   &&
                   _seriesIds.Count <= 0;
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(this.Id);
            _seriesIds.UnionWith(this.SeriesId);
        }
        protected override void Process(IServiceProvider provider)
        {
            if (this.HasParameter(this.InputObject))
            {
                _ids.UnionWith(
                    this.InputObject
                        .Where(x => x.EpisodeFileId > 0)
                            .Select(x => x.EpisodeFileId));
            }
            else if (this.HasParameter(this.SeriesInput))
            {
                _seriesIds.UnionWith(
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

            List<EpisodeFileObject> files = ParameterNameStartsWithSeries(this.ParameterSetName)
                ? this.GetEpFilesBySeriesId(_seriesIds)
                : this.GetEpFilesById(_ids);

            this.WriteCollection(files);
        }

        private List<EpisodeFileObject> GetEpFilesById(IReadOnlySet<int>? fileIds)
        {
            List<EpisodeFileObject> list = [];
            if (fileIds is null)
            {
                return list;
            }

            foreach (int id in fileIds)
            {
                string url = this.Tag.GetUrlForId(id);
                var response = this.SendGetRequest<EpisodeFileObject>(url);
                if (response.IsError)
                {
                    this.WriteConditionalError(response.Error);
                    continue;
                }

                list.Add(response.Data);
            }

            return list;
        }
        private List<EpisodeFileObject> GetEpFilesBySeriesId(IReadOnlySet<int>? seriesIds)
        {
            List<EpisodeFileObject> list = [];
            if (seriesIds is null)
            {
                return list;
            }

            foreach (int id in seriesIds)
            {
                _params.Add(Constants.SERIES_ID_LOWERCASE, id);
                string url = this.Tag.GetUrl(_params);
                var response = this.SendGetRequest<MetadataList<EpisodeFileObject>>(url);
                if (response.IsError)
                {
                    this.WriteConditionalError(response.Error);
                    continue;
                }

                list.AddRange(response.Data);

                _params.Clear();
            }

            return list;
        }
        private static bool ParameterNameStartsWithSeries(ReadOnlySpan<char> setName)
        {
            return setName.StartsWith(
                ['b', 'y', 's', 'e', 'r', 'i', 'e', 's'], StringComparison.OrdinalIgnoreCase);
        }

        protected override void Dispose(bool disposing, IServiceScopeFactory? factory)
        {
            if (disposing && !_disposed)
            {
                if (factory is not null)
                {
                    using var scope = factory.CreateScope();
                    var pool = scope.ServiceProvider.GetService<IObjectPool<SortedSet<int>>>();
                    pool?.Return(_ids);
                    pool?.Return(_seriesIds);
                }
                
                _ids = null!;
                _seriesIds = null!;
                _disposed = true;
            }

            base.Dispose(disposing, factory);
        }
    }
}
