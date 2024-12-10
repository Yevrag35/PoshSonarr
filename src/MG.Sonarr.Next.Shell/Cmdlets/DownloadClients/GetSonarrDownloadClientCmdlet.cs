using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.DownloadClients;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.DownloadClients
{
    [Cmdlet(VerbsCommon.Get, "SonarrDownloadClient", DefaultParameterSetName = "ByNameOrId")]
    public sealed class GetSonarrDownloadClientCmdlet : SonarrMetadataCmdlet
    {
        const int CAPACITY = 2;
        SortedSet<int> _ids = null!;
        HashSet<Wildcard> _wcNames = null!;
        protected override int Capacity => CAPACITY;

        [Parameter(Mandatory = true, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        public int[] Id { get; set; } = [];

        [Parameter(Position = 0, ParameterSetName = "ByNameOrId")]
        public Either<string, int>[] Name { get; set; } = [];

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);

            _ids = this.GetPooledObject<SortedSet<int>>();
            _wcNames = this.GetPooledObject<HashSet<Wildcard>>();

            this.SetReturnables(_ids, _wcNames);
        }
        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.DOWNLOAD_CLIENT];
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(this.Id);
            if (this.MyInvocation.BoundParameters.ContainsKey(nameof(this.Name)))
            {
                this.Name.SplitToSets(_ids, _wcNames);
            }
        }
        protected override void Process(IServiceProvider provider)
        {
            IEnumerable<DownloadClientObject> dlObjs = _ids.Count > 0 && _wcNames.Count == 0
                ? this.GetById<DownloadClientObject>(_ids)
                : this.GetByName(_wcNames, _ids);

            this.WriteCollection(dlObjs);
        }

        private MetadataList<DownloadClientObject> GetByName(HashSet<Wildcard> names, SortedSet<int> ids)
        {
            var all = this.GetAll<DownloadClientObject>();
            if (all.Count > 0 && (names.Count > 0 || ids.Count > 0))
            {
                _ = all.RemoveAll(x => !ids.Contains(x.Id) && !names.AnyValueLike(x.Name));
            }

            return all;
        }
    }
}
