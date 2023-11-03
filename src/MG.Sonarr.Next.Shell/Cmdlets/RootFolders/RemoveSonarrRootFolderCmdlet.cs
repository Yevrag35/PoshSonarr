using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.RootFolders;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.RootFolders
{
    [Cmdlet(VerbsCommon.Remove, "SonarrRootFolder", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true,
        DefaultParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
    [MetadataCanPipe(Tag = Meta.ROOT_FOLDER)]
    public sealed class RemoveSonarrRootFolderCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSET_PIPELINE)]
        public RootFolderObject[] InputObject { get; set; } = Array.Empty<RootFolderObject>();

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = Array.Empty<int>();

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override int Capacity => 1;
        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.ROOT_FOLDER];
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
            if (!this.HasParameter(x => x.InputObject))
            {
                return;
            }

            foreach (RootFolderObject rootFol in this.InputObject)
            {
                if (rootFol.Id <= 0)
                {
                    continue;
                }

                _ = _ids.Add(rootFol.Id);
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
                this.PerformDelete(in id, in force);
            }
        }

        private void PerformDelete(in int id, in bool force)
        {
            string url = this.Tag.GetUrlForId(id);

            if (!force
                &&
                !this.ShouldProcess(url, "Deleting Root Folder"))
            {
                return;
            }

            var response = this.SendDeleteRequest(url);
            if (response.IsError)
            {
                this.WriteError(response.Error);
            }
        }
    }
}

