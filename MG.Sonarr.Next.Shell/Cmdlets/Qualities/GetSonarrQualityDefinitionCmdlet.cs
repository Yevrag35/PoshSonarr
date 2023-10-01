using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Qualities;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Qualities
{
    [Cmdlet(VerbsCommon.Get, "SonarrQualityDefinition")]
    [Alias("Get-SonarrQuality")]
    public sealed class GetSonarrQualityDefinitionCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;
        HashSet<WildcardString> _wcNames = null!;
        List<QualityDefinitionObject> _list = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false)]
        public int[] Id
        {
            get => Array.Empty<int>();
            set => _ids.UnionWith(value.Where(x => x > 0));
        }

        const string NAME = " -Name ";
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0)]
        [SupportsWildcards]
        public IntOrString[] Name
        {
            get => Array.Empty<IntOrString>();
            set
            {
                value.SplitToSets(_ids, _wcNames,
                    this.MyInvocation.Line.Contains(NAME, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        protected override int Capacity => 2;
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            this.Returnables[0] = _ids;
            _wcNames = this.GetPooledObject<HashSet<WildcardString>>();
            this.Returnables[1] = _wcNames;
            _list = new(1);
        }
        protected override MetadataTag GetMetadataTag(MetadataResolver resolver)
        {
            return resolver[Meta.QUALITY_DEFINITION];
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

        private static void FilterByName(List<QualityDefinitionObject> all, IReadOnlySet<int> ids, IReadOnlySet<WildcardString>? names)
        {
            if (names.IsNullOrEmpty())
            {
                return;
            }

            for (int i = all.Count - 1; i >= 0; i--)
            {
                QualityDefinitionObject item = all[i];
                if (ids.Contains(item.Id) || !names.AnyValueLike(item.Title))
                {
                    all.RemoveAt(i);
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
