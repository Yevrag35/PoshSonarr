using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags
{
    [Cmdlet(VerbsCommon.Get, "SonarrTag", DefaultParameterSetName = "ByName")]
    [MetadataCanPipe(Tag = Meta.DELAY_PROFILE)]
    [MetadataCanPipe(Tag = Meta.DOWNLOAD_CLIENT)]
    [MetadataCanPipe(Tag = Meta.INDEXER)]
    [MetadataCanPipe(Tag = Meta.RELEASE_PROFILE)]
    [MetadataCanPipe(Tag = Meta.SERIES)]
    [MetadataCanPipe(Tag = Meta.SERIES_ADD)]
    public sealed class GetSonarrTagCmdlet : SonarrMetadataCmdlet
    {
        const string BY_PIPELINE = "ByPipelineInput";

        SortedSet<int> _ids = null!;
        HashSet<Wildcard> _names = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = BY_PIPELINE)]
        [ValidateIds(ValidateRangeKind.Positive, typeof(ITagPipeable))]
        public ITagPipeable[] InputObject { get; set; } = Array.Empty<ITagPipeable>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        public int[] Id { get; set; } = Array.Empty<int>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByName")]
        [SupportsWildcards]
        public IntOrString[] Name { get; set; } = Array.Empty<IntOrString>();

        protected override int Capacity => 2;
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            _names = this.GetPooledObject<HashSet<Wildcard>>();
            var span = this.GetReturnables();
            span[0] = _ids;
            span[1] = _names;
        }
        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.TAG];
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(this.Id);
            if (this.HasParameter(x => x.Name))
            {
                this.Name.SplitToSets(_ids, _names);
            }
        }
        protected override void Process(IServiceProvider provider)
        {
            if (this.HasParameter(x => x.InputObject))
            {
                _ids.UnionWith(this.InputObject.SelectMany(x => x.Tags)
                    .Where(x => x > 0));
            }
        }
        protected override void End(IServiceProvider provider)
        {
            if (this.InvokeCommand.HasErrors)
            {
                return;
            }

            bool hasIds = this.ProcessIds(_ids);

            if (this.ParameterSetName != BY_PIPELINE && !this.TryProcessNames(_names) && !hasIds)
            {
                SonarrResponse<MetadataList<TagObject>> tags = this.GetAllTags();
                if (tags.IsError)
                {
                    this.StopCmdlet(tags.Error);
                    return;
                }

                this.WriteCollection(tags.Data);
            }
        }
        private SonarrResponse<MetadataList<TagObject>> GetAllTags()
        {
            return this.SendGetRequest<MetadataList<TagObject>>(Constants.TAG);
        }
        private static void ProcessAndFilterTags(IList<TagObject> data, IReadOnlySet<Wildcard> names)
        {
            for (int i = data.Count - 1; i >= 0; i--)
            {
                if (!names.AnyValueLike(data[i].Label))
                {
                    data.RemoveAt(i);
                }
            }
        }
        private bool ProcessIds(IReadOnlyCollection<int> ids)
        {
            bool hasIds = false;
            if (ids.Count > 0)
            {
                hasIds = true;
                foreach (int id in ids)
                {
                    var result = this.SendGetRequest<TagObject>($"/tag/{id}");
                    if (result.IsError)
                    {
                        this.WriteConditionalError(result.Error);
                        continue;
                    }

                    this.WriteObject(result.Data, enumerateCollection: true);
                }
            }

            return hasIds;
        }
        private bool TryProcessNames(IReadOnlySet<Wildcard> names)
        {
            if (names.Count <= 0)
            {
                return false;
            }

            SonarrResponse<MetadataList<TagObject>> list = this.GetAllTags();
            if (list.IsError)
            {
                this.StopCmdlet(list.Error);
                return false;
            }
            else
            {
                ProcessAndFilterTags(list.Data, _names);
                this.WriteCollection(list.Data);
            }
            
            return true;
        }

        bool _disposed;
        protected override void Dispose(bool disposing, IServiceScopeFactory? factory)
        {
            if (disposing && !_disposed)
            {
                _ids = null!;
                _names = null!;
                _disposed = true;
            }

            base.Dispose(disposing, factory);
        }
    }
}
