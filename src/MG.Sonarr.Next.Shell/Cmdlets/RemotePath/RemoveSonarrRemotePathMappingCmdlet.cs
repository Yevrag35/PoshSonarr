using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.RemotePaths;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.RemotePath
{
    [Cmdlet(VerbsCommon.Remove, "SonarrRemotePathMapping", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High, DefaultParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
    [MetadataCanPipe(Tag = Meta.REMOTE_PATH_MAPPING)]
    public sealed class RemoveSonarrRemotePathMappingCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = Array.Empty<int>();

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSET_PIPELINE)]
        [ValidateIds(ValidateRangeKind.Positive)]
        public RemotePathObject[] InputObject { get; set; } = Array.Empty<RemotePathObject>();

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override int Capacity => 1;
        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.REMOTE_PATH_MAPPING];
        }
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            _ids = this.GetPooledObject<SortedSet<int>>();
            this.GetReturnables()[0] = _ids;
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(this.Id);
        }
        protected override void Process(IServiceProvider provider)
        {
            if (PSConstants.PSET_PIPELINE == this.ParameterSetName)
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
                this.DeleteRemotePathMapping(in id, this.Tag, in force);
            }
        }

        private void DeleteRemotePathMapping(in int id, MetadataTag tag, in bool force)
        {
            string url = tag.GetUrlForId(id);
            if (!force
                &&
                !this.ShouldProcess(url, "Deleting Remote Path Mapping"))
            {
                return;
            }

            var response = this.SendDeleteRequest(url);
            if (response.IsError)
            {
                this.WriteError(response.Error);
                return;
            }

            this.WriteVerbose($"Deleted Remote Path Mapping -> {id}");
        }
    }
}

