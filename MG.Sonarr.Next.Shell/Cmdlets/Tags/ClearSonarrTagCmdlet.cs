using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags
{
    [Cmdlet(VerbsCommon.Clear, "SonarrTag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    public sealed class ClearSonarrTagCmdlet : SonarrApiCmdletBase
    {
        readonly HashSet<int> _ids;
        readonly HashSet<WildcardString> _resolveNames;
        readonly Dictionary<string, PSObject> _updates;

        public ClearSonarrTagCmdlet()
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

            if (_updates.Count <= 0)
            {
                throw new ArgumentException(
                    "No tag ID's were provided from the pipeline. Did you mean to use \"Clear-SonarrTag\"?");
            }
        }

        protected override ErrorRecord? Begin()
        {
            if (_resolveNames.Count > 0)
            {
                var tagResponse = this.SendGetRequest<List<SonarrTag>>(Constants.TAG);
                if (tagResponse.IsError)
                {
                    return tagResponse.Error;
                }

                foreach (var tag in tagResponse.Data)
                {
                    if (_resolveNames.ValueLike(tag.Label))
                    {
                        _ = _ids.Add(tag.Id);
                    }
                }
            }

            return null;
        }

        protected override ErrorRecord? End()
        {
            foreach (var kvp in _updates)
            {
                if (!kvp.Value.TryGetNonNullProperty("Tags", out SortedSet<int>? set))
                {
                    this.WriteWarning("Object does not have 'Tags' property.");
                    continue;
                }
                else if (!_ids.Overlaps(set))
                {
                    this.WriteVerbose("No tags are being removed from the object.");
                    continue;
                }

                if (this.ShouldProcess(
                    target: kvp.Key,
                    action: string.Format(
                        "Removing tags: ({0})",
                        string.Join(", ", _ids.Where(set.Contains)))))
                {
                    set.ExceptWith(_ids);

                    var response = this.SendPutRequest(path: kvp.Key, body: kvp.Value);
                    if (response.IsError)
                    {
                        return response.Error;
                    }
                }
            }

            return null;
        }
    }
}
