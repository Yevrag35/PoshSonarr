using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Languages
{
    [Cmdlet(VerbsCommon.Get, "SonarrLanguageProfile", DefaultParameterSetName = "None")]
    [MetadataCanPipe(Tag = Meta.SERIES)]
    [Obsolete("The LanguageProfileController and its endpoints are deprecated.")]
    public sealed class GetSonarrLanguageProfileCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;
        WildcardSet _wcNames = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = PSConstants.PSET_EXPLICIT_ID, Position = 0)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = [];

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSET_PIPELINE, DontShow = true)]
        [ValidateIds(ValidateRangeKind.Positive, typeof(ILanguageProfilePipeable))]
        public ILanguageProfilePipeable[] InputObject { get; set; } = [];

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0)]
        [SupportsWildcards]
        public string[] Name { get; set; } = [];

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
            return resolver[Meta.LANGUAGE];
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(this.Id);
            if (this.HasParameter(this.Name))
            {
                _wcNames.UnionWith(this.Name);
            }
        }
        [SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
        protected override void Process(IServiceProvider provider)
        {
            if (this.InputObject.Length > 0)
            {
                _ids.AddRange(this.InputObject);
            }
        }
        protected override void End(IServiceProvider provider)
        {
            bool addedIds = false;
            if (_ids.Count > 0)
            {
                addedIds = true;
                var fromIds = this.GetById<LanguageProfileObject>(_ids);
                this.WriteCollection(fromIds);
            }

            if (_wcNames.Count > 0 || !addedIds)
            {
                var fromNames = this.GetByName(_wcNames, _ids);
                this.WriteCollection(fromNames);
            }
        }

        private MetadataList<LanguageProfileObject> GetByName(WildcardSet names, SortedSet<int> ids)
        {
            MetadataList<LanguageProfileObject> response = this.GetAll<LanguageProfileObject>();
            if (response.Count == 0 || names.Count == 0)
            {
                return response;
            }

            for (int i = response.Count - 1; i >= 0; i--)
            {
                var item = response[i];
                if (ids.Contains(item.Id) || !names.IsAnyMatch(item.Name))
                {
                    response.RemoveAt(i);
                }
            }

            return response;
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
