﻿using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Qualities;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Qualities
{
    [Cmdlet(VerbsCommon.Get, "SonarrQualityProfile", DefaultParameterSetName = "ByProfileName")]
    [Alias("Get-SonarrProfile")]
    [MetadataCanPipe(Tag = Meta.SERIES)]
    public sealed class GetSonarrQualityProfileCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;
        HashSet<Wildcard> _wcNames = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = PSConstants.PSET_PIPELINE, ValueFromPipeline = true)]
        [ValidateIds(ValidateRangeKind.Positive, typeof(IQualityProfilePipeable))]
        public IQualityProfilePipeable[] InputObject { get; set; } = Array.Empty<IQualityProfilePipeable>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = Array.Empty<int>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByProfileName")]
        [SupportsWildcards]
        public IntOrString[] Name { get; set; } = Array.Empty<IntOrString>();

        protected override int Capacity => 2;
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            _wcNames = this.GetPooledObject<HashSet<Wildcard>>();
            var span = this.GetReturnables();
            span[0] = _ids;
            span[1] = _wcNames;
        }

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.QUALITY_PROFILE];
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
                _ids.UnionWith(
                    this.InputObject.Where(x => x.QualityProfileId > 0).Select(x => x.QualityProfileId));
            }
        }
        protected override void End(IServiceProvider provider)
        {
            if (this.InvokeCommand.HasErrors)
            {
                return;
            }

            List<QualityProfileObject> list = new(_ids.Count + _wcNames.Count);

            bool addedIds = false;
            if (_ids.Count > 0)
            {
                IEnumerable<QualityProfileObject> profiles = this.GetById<QualityProfileObject>(_ids);
                list.AddRange(profiles);
                addedIds = true;
            }

            if (_wcNames.Count > 0 || !addedIds)
            {
                var all = this.GetAll<QualityProfileObject>();
                if (all.Count > 0)
                {
                    FilterByProfileName(all, _wcNames, _ids);
                }

                list.AddRange(all);
            }

            this.WriteCollection(list);
        }
        private static void FilterByProfileName(IList<QualityProfileObject> list, IReadOnlySet<Wildcard> names, IReadOnlySet<int> ids)
        {
            if (ids.Count <= 0 && names.Count <= 0)
            {
                return;
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                QualityProfileObject item = list[i];
                if (ids.Contains(item.Id) || !names.AnyValueLike(item.Name))
                {
                    list.RemoveAt(i);
                }
            }
        }

        bool _disposed;
        protected override void Dispose(bool disposing, IServiceScopeFactory? factory)
        {
            if (disposing && !_disposed)
            {
                _ids = null!;
                _wcNames = null!;
                _disposed = true;
            }

            base.Dispose(disposing, factory);
        }
    }
}
