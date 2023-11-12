using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Releases
{
    [Cmdlet(VerbsCommon.Get, "SonarrReleaseProfile")]
    public sealed class GetSonarrReleaseProfileCmdlet : SonarrMetadataCmdlet
    {
        static readonly string _namePropertyName = nameof(Name);
        SortedSet<int> _ids = null!;
        WildcardSet _wcNames = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = [];

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByProfileNameOrId")]
        [SupportsWildcards]
        public Either<string, int>[] Name { get; set; } = [];

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
            return resolver[Meta.RELEASE_PROFILE];
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(this.Id);
            if (this.HasParameter(this.Name))
            {
                this.Name.SplitToSets(_ids, _wcNames, !this.MyInvocation.IsBoundPositionally(_namePropertyName));
            }
        }
        protected override void Process(IServiceProvider provider)
        {
            IList<ReleaseProfileObject> profiles = _ids.Count > 0
                ? this.GetById<ReleaseProfileObject>(_ids)
                : this.GetByName(_wcNames);

            this.WriteCollection(profiles);
        }

        private MetadataList<ReleaseProfileObject> GetByName(WildcardSet names)
        {
            var response = this.SendGetRequest<MetadataList<ReleaseProfileObject>>(this.Tag.UrlBase);
            if (response.IsError)
            {
                this.StopCmdlet(response.Error);
                return [];
            }
            else if (names.IsNullOrEmpty())
            {
                return response.Value;
            }

            for (int i = response.Value.Count - 1; i >= 0; i--)
            {
                var profile = response.Value[i];
                if (!profile.TryGetNonNullProperty(Constants.NAME, out string? name)
                    ||
                    !names.IsAnyMatch(name))
                {
                    response.Value.RemoveAt(i);
                }
            }

            return response.Value;
        }
    }
}
