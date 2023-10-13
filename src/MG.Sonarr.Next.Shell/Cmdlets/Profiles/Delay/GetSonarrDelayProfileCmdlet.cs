﻿using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Delays
{
    [Cmdlet(VerbsCommon.Get, "SonarrDelayProfile")]
    public sealed class GetSonarrDelayProfileCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = Array.Empty<int>();

        protected override int Capacity => 1;

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = GetPooledObject<SortedSet<int>>();
            Returnables[0] = _ids;
        }

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.DELAY_PROFILE];
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(Id);
        }
        protected override void Process(IServiceProvider provider)
        {
            IEnumerable<DelayProfileObject> profiles = _ids.Count > 0
                ? GetById<DelayProfileObject>(_ids)
                : GetAll<DelayProfileObject>();

            this.WriteCollection(profiles);
        }
    }
}