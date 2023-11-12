using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.RootFolders;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.RootFolders
{
    [Cmdlet(VerbsCommon.Get, "SonarrRootFolder")]
    public sealed class GetSonarrRootFolderCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = Array.Empty<int>();

        protected override int Capacity => 1;
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            this.GetReturnables()[0] = _ids;
        }

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.ROOT_FOLDER];
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(this.Id);
        }

        protected override void Process(IServiceProvider provider)
        {
            IEnumerable<RootFolderObject> folders = this.HasParameter(x => x.Id)
                ? this.GetById<RootFolderObject>(_ids)
                : this.GetAll<RootFolderObject>();

            this.WriteCollection(folders);
        }

        bool _disposed;
        protected override void Dispose(bool disposing, IServiceScopeFactory? factory)
        {
            base.Dispose(disposing, factory);
            if (disposing && !_disposed)
            {
                _ids = null!;
                _disposed = true;
            }
        }
    }
}
