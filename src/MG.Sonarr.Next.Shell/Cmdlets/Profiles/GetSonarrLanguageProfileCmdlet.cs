using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Profiles;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles
{
    [Cmdlet(VerbsCommon.Get, "SonarrLanguageProfile", DefaultParameterSetName = "None")]
    public sealed class GetSonarrLanguageProfileCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;
        HashSet<Wildcard> _wcNames = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = "ByProfileId")]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = Array.Empty<int>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = "ByPipelineInput", DontShow = true)]
        public ILanguageProfilePipeable[] InputObject { get; set; } = Array.Empty<ILanguageProfilePipeable>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0)]
        [SupportsWildcards]
        public IntOrString[] Name { get; set; } = Array.Empty<IntOrString>();

        protected override int Capacity => 2;
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            this.Returnables[0] = _ids;
            _wcNames = this.GetPooledObject<HashSet<Wildcard>>();
            this.Returnables[1] = _wcNames;
        }

        protected override MetadataTag GetMetadataTag(MetadataResolver resolver)
        {
            return resolver[Meta.LANGUAGE];
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(this.Id);
            if (this.HasParameter(x => x.Name))
            {
                this.Name.SplitToSets(_ids, _wcNames,
                    this.MyInvocation.Line.Contains(" -Name ", StringComparison.InvariantCultureIgnoreCase));
            }
        }
        protected override void Process(IServiceProvider provider)
        {
            if (this.HasParameter(x => x.InputObject))
            {
                _ids.UnionWith(this.InputObject.Select(x => x.LanguageProfileId));
            }
        }
        protected override void End(IServiceProvider provider)
        {
            bool addedIds = false;
            if (_ids.Count > 0)
            {
                addedIds = true;
                var fromIds = this.GetById<LanguageProfileObject>(_ids);
                this.WriteCollection(fromIds);
            }

            if (_wcNames.Count > 0 || !addedIds)
            {
                var fromNames = this.GetByName(_wcNames, _ids);
                this.WriteCollection(fromNames);
            }
        }

        private IEnumerable<LanguageProfileObject> GetByName(IReadOnlySet<Wildcard> names, IReadOnlySet<int> ids)
        {
            var response = this.GetAll<LanguageProfileObject>();
            if (response.Count <= 0 || names.Count <= 0)
            {
                return response;
            }

            for (int i = response.Count - 1; i >= 0; i--)
            {
                var item = response[i];
                if (ids.Contains(item.Id) || !names.AnyValueLike(item.Name))
                {
                    response.RemoveAt(i);
                }
            }

            return response;
        }

        bool _disposed;
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _ids = null!;
                _wcNames = null!;
                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
