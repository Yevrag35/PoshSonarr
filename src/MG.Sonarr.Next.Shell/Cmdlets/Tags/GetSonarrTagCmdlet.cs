using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags
{
    [Cmdlet(VerbsCommon.Get, "SonarrTag", DefaultParameterSetName = "ByName")]
    [MetadataCanPipe(Tag = Meta.DELAY_PROFILE)]
    [MetadataCanPipe(Tag = Meta.DOWNLOAD_CLIENT)]
    [MetadataCanPipe(Tag = Meta.INDEXER)]
    [MetadataCanPipe(Tag = Meta.RELEASE_PROFILE)]
    [MetadataCanPipe(Tag = Meta.SERIES)]
    [MetadataCanPipe(Tag = Meta.SERIES_ADD)]
    public sealed class GetSonarrTagCmdlet : SonarrMetadataCmdlet
    {
        const string BY_PIPELINE = "ByPipelineInput";
        static readonly string _namePropertyName = nameof(Name);

        SortedSet<int> _ids = null!;
        WildcardSet _wcNames = null!;

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = BY_PIPELINE)]
        [ValidateIds(ValidateRangeKind.Positive, typeof(ITagPipeable))]
        public ITagPipeable[] InputObject { get; set; } = [];

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = [];

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByName")]
        [SupportsWildcards]
        [ValidateIds(ValidateRangeKind.Positive, NullBehavior = InputNullBehavior.Ignore)]
        public Either<string, int>[] Name { get; set; } = [];

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
            return resolver[Meta.TAG];
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
            if (this.HasParameter(this.InputObject))
            {
                _ids.UnionWith(this.InputObject.SelectMany(x => x.Tags)
                                               .Where(x => x > 0));
            }
        }
        protected override void End(IServiceProvider provider)
        {
            if (this.InvokeCommand.HasErrors)
            {
                return;
            }

            IList<TagObject> tags = _ids.Count > 0 && _wcNames.Count == 0
                ? this.GetById<TagObject>(_ids)
                : this.GetAll<TagObject>();

            this.WriteCollection(tags);
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
