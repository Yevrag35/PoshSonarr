using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.DownloadClients;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;

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
        public int[] Id { get; set; } = Array.Empty<int>();

        [Parameter(Position = 0, ParameterSetName = "ByNameOrId")]
        public IntOrString[] Name { get; set; } = Array.Empty<IntOrString>();

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            //this.Returnables[0] = _ids;

            _wcNames = this.GetPooledObject<HashSet<Wildcard>>();
            //this.Returnables[1] = _wcNames;

            var span = this.GetReturnables();
            span[0] = _ids;
            span[1] = _wcNames;
        }
        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.DOWNLOAD_CLIENT];
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(this.Id);
            if (this.HasParameter(x => x.Name))
            {
                this.Name.SplitToSets(_ids, _wcNames);
            }
        }
        protected override void Process(IServiceProvider provider)
        {
            IEnumerable<DownloadClientObject> dlObjs = _ids.Count > 0
                ? this.GetById<DownloadClientObject>(_ids)
                : this.GetByName(_wcNames, _ids);

            this.WriteCollection(dlObjs);
        }

        private IEnumerable<DownloadClientObject> GetByName(IReadOnlySet<Wildcard> names, IReadOnlySet<int> ids)
        {
            var all = this.GetAll<DownloadClientObject>();
            if (names.Count <= 0 || all.Count <= 0)
            {
                return all;
            }
            
            for (int i = all.Count - 1; i >= 0; i--)
            {
                DownloadClientObject item = all[i];
                if (_ids.Contains(item.Id)
                    ||
                    !_wcNames.AnyValueLike(item.Name))
                {
                    all.RemoveAt(i);
                }
            }

            return all;
        }
    }
}
