using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags
{
    [Cmdlet(VerbsCommon.Clear, "SonarrTag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [MetadataCanPipe(Tag = Meta.DELAY_PROFILE)]
    [MetadataCanPipe(Tag = Meta.DOWNLOAD_CLIENT)]
    [MetadataCanPipe(Tag = Meta.INDEXER)]
    [MetadataCanPipe(Tag = Meta.RELEASE_PROFILE)]
    [MetadataCanPipe(Tag = Meta.SERIES)]
    [MetadataCanPipe(Tag = Meta.SERIES_ADD)]
    public sealed class ClearSonarrTagCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;
        HashSet<Wildcard> _resolveNames = null!;
        Dictionary<string, ITagPipeable> _updates = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public ITagPipeable[] InputObject { get; set; } = Array.Empty<ITagPipeable>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = "ById")]
        public int[] Id { get; set; } = Array.Empty<int>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByName")]
        public IntOrString[] Name { get; set; } = Array.Empty<IntOrString>();

        protected override int Capacity => 2;
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            this.Returnables[0] = _ids;
            _resolveNames = this.GetPooledObject<HashSet<Wildcard>>();
            this.Returnables[1] = _resolveNames;
            _updates = new(1, StringComparer.InvariantCultureIgnoreCase);
        }

        private static void AddUrlsFromMetadata(ITagPipeable[] array, IDictionary<string, ITagPipeable> updates)
        {
            foreach (ITagPipeable pipeable in array)
            {
                updates.TryAdd(pipeable.MetadataTag.GetUrlForId(pipeable.Id), pipeable);
            }

            if (updates.Count <= 0)
            {
                throw new ArgumentException(
                    "No tag ID's were provided from the pipeline. Did you mean to use \"Clear-SonarrTag\"?");
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
                AddUrlsFromMetadata(this.InputObject, _updates);
            }
        }
        protected override void End(IServiceProvider provider)
        {
            if (this.InvokeCommand.HasErrors)
            {
                return;
            }

            foreach (var kvp in _updates)
            {
                if (!_ids.Overlaps(kvp.Value.Tags))
                {
                    this.WriteVerbose("No tags are being removed from the object.");
                    continue;
                }

                if (this.ShouldProcess(
                    target: kvp.Key,
                    action: string.Format(
                        "Removing tags: ({0})",
                        string.Join(", ", _ids.Where(kvp.Value.Tags.Contains)))))
                {
                    kvp.Value.Tags.ExceptWith(_ids);

                    var response = this.SendPutRequest(path: kvp.Key, body: kvp.Value);
                    if (response.IsError)
                    {
                        kvp.Value.Reset();
                        this.WriteConditionalError(response.Error);
                    }
                    else
                    {
                        kvp.Value.CommitTags();
                    }
                }
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
