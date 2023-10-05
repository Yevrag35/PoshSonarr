using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Tags;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags
{
    [Cmdlet(VerbsCommon.Add, "SonarrTag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    public sealed class AddSonarrTagCmdlet : SonarrMetadataCmdlet
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

        private void AddUrlsFromMetadata(ITagPipeable[] pipeables)
        {
            foreach (ITagPipeable item in pipeables)
            {
                bool added = _updates.TryAdd(item.MetadataTag.GetUrlForId(item.Id), item);
            }
        }

        protected override MetadataTag GetMetadataTag(MetadataResolver resolver)
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

            foreach (var kvp in _updates)
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
                    kvp.Value.Tags.UnionWith(_ids);

                    var response = this.SendPutRequest(path: kvp.Key, body: kvp.Value);
                    if (response.IsError)
                    {
                        kvp.Value.Reset();
                        this.WriteConditionalError(response.Error);
                        continue;
                    }
                    else
                    {
                        kvp.Value.CommitTags();
                    }
                }
            }
        }

        bool _disposed;
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _ids = null!;
                _resolveNames = null!;
                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
