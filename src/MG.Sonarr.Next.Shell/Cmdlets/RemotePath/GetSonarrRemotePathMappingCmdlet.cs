using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.RemotePaths;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.RemotePath
{
    [Cmdlet(VerbsCommon.Get, "SonarrRemotePathMapping")]
    [Alias("Get-SonarrRemotePath", "Get-SonarrRemoteMapping")]
    public sealed class GetSonarrRemotePathMappingCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;

        [Parameter(Mandatory = false, Position = 0)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = Array.Empty<int>();

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
            IEnumerable<RemotePathObject> remotePaths = _ids.Count > 0
                ? this.GetById<RemotePathObject>(_ids)
                : this.GetAll<RemotePathObject>();

            this.WriteCollection(remotePaths);
        }
    }
}

