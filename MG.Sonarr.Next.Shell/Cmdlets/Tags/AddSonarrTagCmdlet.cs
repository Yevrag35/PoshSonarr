using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Tags;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Models.Tags;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags
{
    [Cmdlet(VerbsCommon.Add, "SonarrTag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    public sealed class AddSonarrTagCmdlet : SonarrApiCmdletBase
    {
        readonly HashSet<int> _ids;
        readonly HashSet<WildcardString> _resolveNames;
        readonly Dictionary<string, PSObject> _updates;

        public AddSonarrTagCmdlet()
            : base()
        {
            _ids = new(1);
            _resolveNames = new(1);
            _updates = new(1, StringComparer.InvariantCultureIgnoreCase);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public object[] InputObject
        {
            get => Array.Empty<object>();
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

        private void AddUrlsFromMetadata(object[]? array)
        {
            if (array is null)
            {
                return;
            }

            foreach (PSObject pso in array.OfType<PSObject>())
            {
                if (pso.TryGetNonNullProperty(Constants.META_PROPERTY_NAME, out MetadataTag? tag)
                    &&
                    tag.SupportsId
                    &&
                    pso.TryGetProperty(Constants.ID, out int id))
                {
                    _updates.TryAdd(tag.GetUrlForId(id), pso);
                }
            }
        }

        protected override void Begin()
        {
            if (_resolveNames.Count > 0)
            {
                var tagResponse = this.SendGetRequest<MetadataList<TagObject>>(Constants.TAG);
                if (tagResponse.IsError)
                {
                    this.StopCmdlet(tagResponse.Error);
                    return;
                }

                foreach (var tag in tagResponse.Data)
                {
                    if (_resolveNames.ValueLike(tag.Label))
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
                if (!kvp.Value.TryGetNonNullProperty("Tags", out SortedSet<int>? set))
                {
                    this.WriteWarning("Object does not have a 'Tags' property.");
                    continue;
                }
                else if (set.IsSupersetOf(_ids))
                {
                    this.WriteVerbose("No tags are being added that didn't already exist on the object.");
                    continue;
                }

                if (this.ShouldProcess(
                    target: kvp.Key, 
                    action: string.Format(
                        "Adding tags: ({0})",
                        string.Join(", ", _ids.Where(x => !set.Contains(x))))))
                {
                    set.UnionWith(_ids);

                    var response = this.SendPutRequest(path: kvp.Key, body: kvp.Value);
                    if (response.IsError)
                    {
                        this.WriteConditionalError(response.Error);
                        continue;
                    }
                }
            }
        }
    }
}
