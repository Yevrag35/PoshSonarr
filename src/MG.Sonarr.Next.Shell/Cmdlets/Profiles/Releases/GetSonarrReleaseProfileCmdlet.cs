using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Releases
{
    [Cmdlet(VerbsCommon.Get, "SonarrReleaseProfile")]
    public sealed class GetSonarrReleaseProfileCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;
        HashSet<Wildcard> _wcNames = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = "ByProfileId")]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = Array.Empty<int>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByProfileNameOrId")]
        [SupportsWildcards]
        public IntOrString[] Name { get; set; } = Array.Empty<IntOrString>();

        protected override int Capacity => 2;
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = GetPooledObject<SortedSet<int>>();
            Returnables[0] = _ids;
            _wcNames = GetPooledObject<HashSet<Wildcard>>();
            Returnables[1] = _wcNames;
        }

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.RELEASE_PROFILE];
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(Id);
            if (this.HasParameter(x => x.Name))
            {
                Name.SplitToSets(_ids, _wcNames,
                    MyInvocation.Line.Contains(" -Name ", StringComparison.InvariantCultureIgnoreCase));
            }
        }
        protected override void Process(IServiceProvider provider)
        {
            IEnumerable<ReleaseProfileObject> profiles = _ids.Count > 0
                ? GetById<ReleaseProfileObject>(_ids)
                : GetByName(_wcNames);

            this.WriteCollection(profiles);
        }

        private IEnumerable<ReleaseProfileObject> GetByName(IReadOnlySet<Wildcard>? names)
        {
            var response = SendGetRequest<MetadataList<ReleaseProfileObject>>(Tag.UrlBase);
            if (response.IsError)
            {
                StopCmdlet(response.Error);
                return Enumerable.Empty<ReleaseProfileObject>();
            }
            else if (names.IsNullOrEmpty())
            {
                return response.Data;
            }

            for (int i = response.Data.Count - 1; i >= 0; i--)
            {
                var profile = response.Data[i];
                if (!profile.TryGetNonNullProperty(Constants.NAME, out string? name)
                    ||
                    !names.AnyValueLike(name))
                {
                    response.Data.RemoveAt(i);
                }
            }

            return response.Data;
        }

        bool _disposed;
        protected override void Dispose(bool disposing, IServiceScopeFactory? factory)
        {
            if (disposing && !_disposed)
            {
                Returner.Return(Returnables.AsSpan(0, 2));
                _ids = null!;
                _wcNames = null!;
                _disposed = true;
            }

            base.Dispose(disposing, factory);
        }
    }
}
