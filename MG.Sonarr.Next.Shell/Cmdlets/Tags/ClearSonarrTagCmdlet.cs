using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Tags;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags
{
    [Cmdlet(VerbsCommon.Clear, "SonarrTag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    public sealed class ClearSonarrTagCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids;
        HashSet<WildcardString> _resolveNames;
        readonly Dictionary<string, ITagPipeable> _updates;

        public ClearSonarrTagCmdlet()
            : base(2)
        {
            _ids = this.GetPooledObject<SortedSet<int>>();
            this.Returnables[0] = _ids;
            _resolveNames = this.GetPooledObject<HashSet<WildcardString>>();
            this.Returnables[1] = _resolveNames;
            _updates = new(1, StringComparer.InvariantCultureIgnoreCase);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public ITagPipeable[] InputObject
        {
            get => Array.Empty<ITagPipeable>();
            set => this.AddUrlsFromMetadata(value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = "ById")]
        public int[] Id
        {
            get => Array.Empty<int>();
            set => _ids.UnionWith(value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByName")]
        public IntOrString[] Name
        {
            get => Array.Empty<IntOrString>();
            set => value.SplitToSets(_ids, _resolveNames);
        }

        private void AddUrlsFromMetadata(ITagPipeable[]? array)
        {
            if (array is null)
            {
                return;
            }

            foreach (ITagPipeable pipeable in array)
            {
                _updates.TryAdd(pipeable.MetadataTag.GetUrlForId(pipeable.Id), pipeable);
            }

            if (_updates.Count <= 0)
            {
                throw new ArgumentException(
                    "No tag ID's were provided from the pipeline. Did you mean to use \"Clear-SonarrTag\"?");
            }
        }

        protected override MetadataTag GetMetadataTag(MetadataResolver resolver)
        {
            return resolver[Meta.TAG];
        }

        protected override void Begin()
        {
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
        protected override void End()
        {
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
