using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.DownloadClients;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.DownloadClients
{
    [Cmdlet(VerbsCommon.Remove, "SonarrDownloadClient", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true,
        DefaultParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
    public sealed class RemoveSonarrDownloadClientCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;
        protected override int Capacity => 1;

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = Array.Empty<int>();

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSET_PIPELINE)]
        [ValidateIds(ValidateRangeKind.Positive)]
        public DownloadClientObject[] InputObject { get; set; } = Array.Empty<DownloadClientObject>();

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.DOWNLOAD_CLIENT];
        }
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            this.GetReturnables()[0] = _ids;
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

            bool force = this.Force.ToBool();
            foreach (int id in _ids)
            {
                this.DeleteDownloadClient(id, this.Tag, in force);
            }
        }

        private void DeleteDownloadClient(int id, MetadataTag tag, in bool force)
        {
            string url = tag.GetUrlForId(id);
            if (!force
                &&
                !this.ShouldProcess(url, "Delete Download Client"))
            {
                return;
            }

            var response = this.SendDeleteRequest(url);
            if (response.IsError)
            {
                this.WriteError(response.Error);
                return;
            }

            this.WriteVerbose($"Deleted Download Client -> {id}");
        }
    }
}

