﻿using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Qualities;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Qualities
{
    [Cmdlet(VerbsCommon.Get, "SonarrQualityProfile", DefaultParameterSetName = "ByProfileName")]
    [Alias("Get-SonarrProfile")]
    public sealed class GetSonarrQualityProfileCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids;
        HashSet<WildcardString> _wcNames;

        public GetSonarrQualityProfileCmdlet()
            : base(2)
        {
            _ids = this.GetPooledObject<SortedSet<int>>();
            this.Returnables[0] = _ids;
            _wcNames = this.GetPooledObject<HashSet<WildcardString>>();
            this.Returnables[1] = _wcNames;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = "ByPipelineInput", ValueFromPipeline = true)]
        public IQualityProfilePipeable[] InputObject
        {
            get => Array.Empty<IQualityProfilePipeable>();
            set => _ids.UnionWith(value.Where(x => x.QualityProfileId > 0).Select(x => x.QualityProfileId));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = "ByProfileId")]
        public int[] Id
        {
            get => Array.Empty<int>();
            set => _ids.UnionWith(value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByProfileName")]
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
            return resolver[Meta.QUALITY_PROFILE];
        }

        protected override void End()
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
        private static void FilterByProfileName(List<QualityProfileObject> list, IReadOnlySet<WildcardString> names, IReadOnlySet<int> ids)
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
