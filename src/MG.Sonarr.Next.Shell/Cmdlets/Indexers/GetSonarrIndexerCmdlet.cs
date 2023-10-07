using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Indexers;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Indexers
{
    [Cmdlet(VerbsCommon.Get, "SonarrIndexer")]
    public sealed class GetSonarrIndexerCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;
        HashSet<Wildcard> _wcNames = null!;

        [Parameter(Mandatory = true, ParameterSetName = "ByIndexerId")]
        public int[] Id { get; set; } = Array.Empty<int>();

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByIndexerNameOrId")]
        public IntOrString[] Name { get; set; } = Array.Empty<IntOrString>();
         
        protected override int Capacity => 2;
        protected override MetadataTag GetMetadataTag(MetadataResolver resolver)
        {
            return resolver[Meta.INDEXER];
        }
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            _wcNames = this.GetPooledObject<HashSet<Wildcard>>();
            this.Returnables[0] = _ids;
            this.Returnables[1] = _wcNames;
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(this.Id);
            if (this.HasParameter(x => x.Name))
            {
                this.Name.SplitToSets(_ids, _wcNames,
                    this.MyInvocation.Line.Contains(" -Name ", StringComparison.OrdinalIgnoreCase));
            }
        }

        protected override void Process(IServiceProvider provider)
        {
            IEnumerable<IndexerObject> indexers = _ids.Count > 0
                ? this.GetById<IndexerObject>(_ids)
                : this.GetByName(_wcNames, _ids);

            this.WriteCollection(indexers);
        }

        private IEnumerable<IndexerObject> GetByName(IReadOnlySet<Wildcard> names, IReadOnlySet<int> ids)
        {
            var response = this.GetAll<IndexerObject>();
            if (response.Count <= 0 || names.Count <= 0)
            {
                return response;
            }

            for (int i = response.Count - 1; i >= 0; i--)
            {
                var item = response[i];
                if (ids.Contains(item.Id)
                    ||
                    !names.AnyValueLike(item.Name))
                {
                    response.RemoveAt(i);
                }
            }

            return response;
        }
    }
}
