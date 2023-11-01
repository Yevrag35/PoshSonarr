using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Indexers;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Indexers
{
    [Cmdlet(VerbsCommon.Remove, "SonarrIndexer", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true,
        DefaultParameterSetName = "ByExplicitId")]
    [MetadataCanPipe(Tag = Meta.INDEXER)]
    public sealed class RemoveSonarrIndexerCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByExplicitId")]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = Array.Empty<int>();

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByPipelineInput")]
        [ValidateId(ValidateRangeKind.Positive)]
        public IndexerObject[] InputObject { get; set; } = Array.Empty<IndexerObject>();

        [Parameter]
        public SwitchParameter Force { get; set; }
        protected override int Capacity => 1;

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.INDEXER];
        }
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            this.Returnables[0] = _ids;
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(this.Id);
        }

        protected override void Process(IServiceProvider provider)
        {
            if (this.HasParameter(x => x.InputObject))
            {
                _ids.UnionWith(this.InputObject.Select(x => x.Id));
            }
        }

        protected override void End(IServiceProvider provider)
        {
            if (_ids.Count <= 0)
            {
                return;
            }

            foreach (int id in _ids)
            {
                this.DeleteIndexer(id);
            }
        }

        private void DeleteIndexer(int id)
        {
            string url = this.Tag.GetUrlForId(id);
            if (!this.Force
                &&
                !this.ShouldProcess(url, "Deleting Indexer"))
            {
                return;
            }

            var response = this.SendDeleteRequest(url);
            if (response.IsError)
            {
                this.WriteError(response.Error);
                return;
            }

            this.WriteVerbose($"Deleted Indexer -> {id}");
        }
    }
}

