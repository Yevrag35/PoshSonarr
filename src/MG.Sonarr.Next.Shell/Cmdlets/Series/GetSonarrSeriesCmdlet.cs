using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Shell.Output;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Unions;
using MG.Sonarr.Next.Extensions;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsCommon.Get, "SonarrSeries", DefaultParameterSetName = "BySeriesName")]
    [MetadataCanPipe(Tag = Meta.CALENDAR)]
    [MetadataCanPipe(Tag = Meta.EPISODE)]
    [MetadataCanPipe(Tag = Meta.EPISODE_FILE)]
    [MetadataCanPipe(Tag = Meta.RENAMABLE)]
    [OutputType(typeof(ISeriesOutput))]
    public sealed class GetSonarrSeriesCmdlet : SonarrMetadataCmdlet
    {
        static readonly string _namePropertyName = nameof(Name);
        SortedSet<int> _ids = null!;
        WildcardSet _wcNames = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "BySeriesName")]
        [SupportsWildcards]
        public Either<string, int>[] Name { get; set; } = [];

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = [];

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = PSConstants.PSET_PIPELINE, DontShow = true,
            ValueFromPipeline = true)]
        [ValidateIds(ValidateRangeKind.Positive, typeof(ISeriesPipeable))]
        public ISeriesPipeable[] InputObject { get; set; } = [];

        protected override int Capacity => 2;
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            _wcNames = this.GetPooledObject<WildcardSet>();
            this.SetReturnables(_ids, _wcNames);
        }
        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.SERIES];
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(this.Id);
        }
        protected override void Process(IServiceProvider provider)
        {
            if (this.InputObject.Length > 0)
            {
                _ids.AddRange(this.InputObject);
                //_ids.UnionWith(this.InputObject.Select(x => x.SeriesId));
            }
            else
            {
                this.Name.SplitToSets(_ids, _wcNames, !this.MyInvocation.IsBoundPositionally(_namePropertyName));
            }

            bool hadIds = false;
            if (_ids.Count > 0)
            {
                hadIds = true;
                this.WriteSeriesById(_ids);
            }

            if (_wcNames.Count > 0)
            {
                var response = this.GetSeriesByName(_wcNames);
                if (response.IsError)
                {
                    this.StopCmdlet(response.Error);
                    return;
                }

                this.WriteCollection(response.Value);
            }
            else if (!hadIds)
            {
                var response = this.GetAllSeries<SeriesObject>();
                if (response.IsError)
                {
                    this.StopCmdlet(response.Error);
                    return;
                }

                this.WriteCollection(response.Value);
            }
        }

        private SonarrClientResult<MetadataList<SeriesObject>> GetSeriesByName(WildcardSet names)
        {
            var result = this.GetAllSeries<SeriesObject>();
            if (result.IsError)
            {
                return result;
            }

            for (int i = result.Value.Count - 1; i >= 0; i--)
            {
                PSObject item = result.Value[i];
                if (!item.TryGetProperty(Constants.TITLE, out string? title)
                    ||
                    title is null
                    ||
                    !names.IsAnyMatch(title))
                {
                    result.Value.RemoveAt(i);
                }
            }

            return result;
        }
        private SonarrClientResult<MetadataList<T>> GetAllSeries<T>()
            where T : PSObject, IComparable<T>, IJsonMetadataTaggable
        {
            return this.SendGetRequest<MetadataList<T>>(this.Tag.UrlBase);
        }
        private void WriteSeriesById(SortedSet<int> ids)
        {
            foreach (int id in ids)
            {
                SonarrClientResult<SeriesObject> result = this.SendGetRequest<SeriesObject>(this.Tag.GetUrlForId(id));
                if (result.IsError)
                {
                    this.WriteConditionalError(result.Error);
                    continue;
                }

                this.WriteObject(result.Value);
            }
        }

        bool _disposed;
        protected override void Dispose(bool disposing, IServiceScopeFactory? factory)
        {
            if (disposing && !_disposed)
            {
                _ids = null!;
                _wcNames = null!;
                _disposed = true;
            }

            base.Dispose(disposing, factory);
        }
    }
}
