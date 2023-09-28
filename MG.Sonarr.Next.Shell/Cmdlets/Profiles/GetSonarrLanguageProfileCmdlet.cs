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
    [Cmdlet(VerbsCommon.Get, "SonarrLanguageProfile")]
    public sealed class GetSonarrLanguageProfileCmdlet : SonarrMetadataCmdlet
    {
        bool _disposed;
        SortedSet<int> _ids;
        readonly List<LanguageProfileObject> _list;
        HashSet<WildcardString> _wcNames;

        public GetSonarrLanguageProfileCmdlet()
            : base(2)
        {
            _ids = this.GetPooledObject<SortedSet<int>>();
            this.Returnables[0] = _ids;
            _wcNames = this.GetPooledObject<HashSet<WildcardString>>();
            this.Returnables[1] = _wcNames;
            _list = new(1);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = "ByProfileId")]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id
        {
            get => Array.Empty<int>();
            set => _ids.UnionWith(value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0)]
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
            return resolver[Meta.LANGUAGE];
        }

        protected override void Process()
        {
            bool addedIds = false;
            if (!_ids.IsNullOrEmpty())
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

                }
            }
        }

        private static void FilterByName(List<LanguageProfileObject> list, IReadOnlySet<WildcardString>? names, IReadOnlySet<int> ids)
        {

        }

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
