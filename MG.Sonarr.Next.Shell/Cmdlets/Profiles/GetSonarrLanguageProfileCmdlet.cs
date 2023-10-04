using MG.Sonarr.Next.Services.Collections;
using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Profiles;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles
{
    [Cmdlet(VerbsCommon.Get, "SonarrLanguageProfile", DefaultParameterSetName = "None")]
    public sealed class GetSonarrLanguageProfileCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;
        List<LanguageProfileObject> _list = null!;
        HashSet<Wildcard> _wcNames = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = "ByProfileId")]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = Array.Empty<int>();

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
            _list = new(1);
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
            bool addedIds = false;
            if (_ids.Count > 0)
            {
                IEnumerable<LanguageProfileObject> byIds = this.GetById<LanguageProfileObject>(_ids);
                _list.AddRange(byIds);
                addedIds = true;
            }

            if (!_wcNames.IsNullOrEmpty() || !addedIds)
            {
                var all = this.GetAll<LanguageProfileObject>();
                if (all.Count > 0)
                {
                    FilterByName(all, _wcNames, _ids);
                }

                _list.AddRange(all);
            }

            this.WriteCollection(_list);
        }

        private static void FilterByName(List<LanguageProfileObject> list, IReadOnlySet<Wildcard>? names, IReadOnlySet<int> ids)
        {

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
