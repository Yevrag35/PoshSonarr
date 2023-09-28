using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.RootFolders;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.RootFolders
{
    [Cmdlet(VerbsCommon.Get, "SonarrRootFolder")]
    public sealed class GetSonarrRootFolderCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids;

        public GetSonarrRootFolderCmdlet()
            : base(1)
        {
            _ids = this.GetPooledObject<SortedSet<int>>();
            this.Returnables[0] = _ids;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id
        {
            get => Array.Empty<int>();
            set => _ids.UnionWith(value);
        }

        protected override MetadataTag GetMetadataTag(MetadataResolver resolver)
        {
            return resolver[Meta.ROOT_FOLDER];
        }

        protected override void Process()
        {
            IEnumerable<RootFolderObject> folders = this.HasParameter(x => x.Id)
                ? this.GetById<RootFolderObject>(_ids)
                : this.GetAll<RootFolderObject>();

            this.WriteCollection(folders);
        }

        bool _disposed;
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && !_disposed)
            {
                _ids = null!;
                _disposed = true;
            }
        }
    }
}
