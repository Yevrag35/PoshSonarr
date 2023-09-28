using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Profiles;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles
{
    [Cmdlet(VerbsCommon.Get, "SonarrReleaseProfile")]
    public sealed class GetSonarrReleaseProfileCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids;
        HashSet<WildcardString> _wcNames;

        public GetSonarrReleaseProfileCmdlet()
            : base(2)
        {
            _ids = this.GetPooledObject<SortedSet<int>>();
            this.Returnables[0] = _ids;
            _wcNames = this.GetPooledObject<HashSet<WildcardString>>();
            this.Returnables[1] = _wcNames;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = "ByProfileId")]
        public int[] Id
        {
            get => Array.Empty<int>();
            set => _ids.UnionWith(value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByProfileNameOrId")]
        [SupportsWildcards]
        public IntOrString[] Name
        {
            get => Array.Empty<IntOrString>();
            set
            {
                value.SplitToSets(_ids, _wcNames,
                    this.MyInvocation.Line.Contains(" -Name ", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        protected override MetadataTag GetMetadataTag(MetadataResolver resolver)
        {
            return resolver[Meta.RELEASE_PROFILE];
        }
        protected override void Process()
        {
            IEnumerable<ReleaseProfileObject> profiles = _ids.Count > 0
                ? this.GetById<ReleaseProfileObject>(_ids)
                : this.GetByName(_wcNames);

            this.WriteCollection(profiles);
        }

        private IEnumerable<ReleaseProfileObject> GetByName(IReadOnlySet<WildcardString>? names)
        {
            var response = this.SendGetRequest<MetadataList<ReleaseProfileObject>>(this.Tag.UrlBase);
            if (response.IsError)
            {
                this.StopCmdlet(response.Error);
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
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                this.Returner.Return(this.Returnables.AsSpan(0, 2));
                _ids = null!;
                _wcNames = null!;
                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
