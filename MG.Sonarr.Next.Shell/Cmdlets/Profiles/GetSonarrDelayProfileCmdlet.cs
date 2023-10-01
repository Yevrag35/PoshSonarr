using MG.Sonarr.Next.Services.Collections;
using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Profiles;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles
{
    [Cmdlet(VerbsCommon.Get, "SonarrDelayProfile")]
    public sealed class GetSonarrDelayProfileCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id
        {
            get => Array.Empty<int>();
            set => _ids.UnionWith(value);
        }

        protected override int Capacity => 1;

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            this.Returnables[0] = _ids;
        }

        protected override MetadataTag GetMetadataTag(MetadataResolver resolver)
        {
            return resolver[Meta.DELAY_PROFILE];
        }

        protected override void Process(IServiceProvider provider)
        {
            IEnumerable<DelayProfileObject> profiles = !_ids.IsNullOrEmpty()
                ? this.GetById<DelayProfileObject>(_ids)
                : this.GetAll<DelayProfileObject>();

            this.WriteCollection(profiles);
        }
    }
}
