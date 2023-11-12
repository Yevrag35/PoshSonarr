using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Qualities;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Qualities
{
    [Cmdlet(VerbsCommon.Get, "SonarrQualityDefinition")]
    [Alias("Get-SonarrQuality")]
    public sealed class GetSonarrQualityDefinitionCmdlet : SonarrMetadataCmdlet
    {
        static readonly string _namePropertyName = nameof(Name);
        SortedSet<int> _ids = null!;
        WildcardSet _wcNames = null!;
        List<QualityDefinitionObject> _list = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = [];

        const string NAME = " -Name ";
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0)]
        [SupportsWildcards]
        public Either<string, int>[] Name { get; set; } = [];

        protected override int Capacity => 2;
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            _wcNames = this.GetPooledObject<WildcardSet>();
            this.SetReturnables(_ids, _wcNames);
            _list = new(1);
        }
        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.QUALITY_DEFINITION];
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
            bool addedIds = false;
            if (_ids.Count > 0)
            {
                _list.AddRange(this.GetById<QualityDefinitionObject>(_ids));
                addedIds = true;
            }

            if (_wcNames.Count > 0 || !addedIds)
            {
                var all = this.GetAll<QualityDefinitionObject>();
                if (all.Count > 0)
                {
                    FilterByName(all, _ids, _wcNames);
                    _list.AddRange(all);
                }
            }
        }
        protected override void End(IServiceProvider provider)
        {
            this.WriteCollection(_list);
        }

        private static void FilterByName(MetadataList<QualityDefinitionObject> all, SortedSet<int> ids, WildcardSet names)
        {
            if (names.Count == 0)
            {
                return;
            }

            for (int i = all.Count - 1; i >= 0; i--)
            {
                QualityDefinitionObject item = all[i];
                if (ids.Contains(item.Id) || !names.IsAnyMatch(item.Title))
                {
                    all.RemoveAt(i);
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
