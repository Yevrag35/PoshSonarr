using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags
{
    [Cmdlet(VerbsCommon.Add, "SonarrTag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true, 
        DefaultParameterSetName = "None")]
    [MetadataCanPipe(Tag = Meta.DELAY_PROFILE)]
    [MetadataCanPipe(Tag = Meta.DOWNLOAD_CLIENT)]
    [MetadataCanPipe(Tag = Meta.INDEXER)]
    [MetadataCanPipe(Tag = Meta.RELEASE_PROFILE)]
    [MetadataCanPipe(Tag = Meta.SERIES)]
    [MetadataCanPipe(Tag = Meta.SERIES_ADD)]
    public sealed class AddSonarrTagCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;
        HashSet<Wildcard> _resolveNames = null!;
        Dictionary<string, ITagPipeable> _updates = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSET_PIPELINE)]
        [ValidateIds(ValidateRangeKind.Positive, typeof(ITagPipeable))]
        public ITagPipeable[] InputObject { get; set; } = Array.Empty<ITagPipeable>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        public int[] Id { get; set; } = Array.Empty<int>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Position = 0)]
        public IntOrString[] Name { get; set; } = Array.Empty<IntOrString>();

        protected override int Capacity => 2;
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            _resolveNames = this.GetPooledObject<HashSet<Wildcard>>();
            var span = this.GetReturnables();
            span[0] = _ids;
            span[1] = _resolveNames;
            _updates = new(1, StringComparer.InvariantCultureIgnoreCase);
        }

        private void AddUrlsFromMetadata(ITagPipeable[] pipeables)
        {
            foreach (ITagPipeable item in pipeables)
            {
                bool added = _updates.TryAdd(item.MetadataTag.GetUrlForId(item.Id), item);
            }
        }

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.TAG];
        }

        protected override void Begin(IServiceProvider provider)
        {
            if (this.HasParameter(x => x.Id))
            {
                _ids.UnionWith(this.Id);
            }
            else if (this.HasParameter(x => x.Name))
            {
                this.Name.SplitToSets(_ids, _resolveNames);
            }

            if (_resolveNames.Count > 0)
            {
                var all = this.GetAll<TagObject>();

                foreach (var tag in all)
                {
                    if (_resolveNames.AnyValueLike(tag.Label))
                    {
                        _ = _ids.Add(tag.Id);
                    }
                }
            }
        }
        protected override void Process(IServiceProvider provider)
        {
            if (this.HasParameter(x => x.InputObject))
            {
                this.AddUrlsFromMetadata(this.InputObject);
            }
        }
        protected override void End(IServiceProvider provider)
        {
            if (this.InvokeCommand.HasErrors)
            {
                return;
            }

            foreach (KeyValuePair<string, ITagPipeable> kvp in _updates)
            {
                if (kvp.Value.Tags.IsSupersetOf(_ids))
                {
                    this.WriteVerbose("No tags are being added that didn't already exist on the object.");
                    continue;
                }

                if (this.ShouldProcess(
                    target: kvp.Key, 
                    action: string.Format(
                        "Adding tags: ({0})",
                        string.Join(", ", _ids.Where(x => !kvp.Value.Tags.Contains(x))))))
                {
                    if (!this.PerformTagUpdate(in kvp))
                    {
                        continue;
                    }
                }
            }
        }

        private bool PerformTagUpdate(in KeyValuePair<string, ITagPipeable> kvp)
        {
            kvp.Value.Tags.UnionWith(_ids);

            if (!kvp.Value.MustUpdateViaApi)
            {
                return true;
            }

            var response = this.SendPutRequest(path: kvp.Key, body: kvp.Value);
            if (response.IsError)
            {
                kvp.Value.Reset();
                this.WriteConditionalError(response.Error);
                return false;
            }
            else
            {
                kvp.Value.CommitTags();
                return true;
            }
        }

        bool _disposed;
        protected override void Dispose(bool disposing, IServiceScopeFactory? factory)
        {
            if (disposing && !_disposed)
            {
                _ids = null!;
                _resolveNames = null!;
                _disposed = true;
            }

            base.Dispose(disposing, factory);
        }
    }
}
