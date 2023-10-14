using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Backups
{
    [Cmdlet(VerbsCommon.Remove, "SonarrBackup", SupportsShouldProcess = true,
        DefaultParameterSetName = "ByExplicitId")]
    [Alias("Delete-SonarrBackup")]
    public sealed class RemoveSonarrBackupCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;
        bool _yesToAll;
        bool _noToAll;

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByExplicitId")]
        public int[] Id { get; set; } = Array.Empty<int>();

        [Parameter(Mandatory = true, ParameterSetName = "ByPipelineInput", ValueFromPipeline = true)]
        public BackupObject[] InputObject { get; set; } = Array.Empty<BackupObject>();

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override int Capacity => 1;

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.BACKUP];
        }
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            this.Returnables[0] = _ids;
        }

        protected override void Process(IServiceProvider provider)
        {
            _ids.UnionWith(this.Id);
            if (this.HasParameter(x => x.InputObject))
            {
                _ids.UnionWith(this.InputObject.Where(x => x.Id > 0).Select(x => x.Id));
            }
        }

        protected override void End(IServiceProvider provider)
        {
            bool force = this.Force.ToBool();
            foreach (int id in _ids)
            {
                string url = this.Tag.GetUrlForId(id);
                if (this.ShouldProcess(url, "Deleting Backup")
                    &&
                    (force
                    ||
                    this.ShouldContinue(query: "Are you sure you want to delete the backup?",
                        caption: $"Delete Backup: {id}",
                        yesToAll: ref _yesToAll,
                        noToAll: ref _noToAll)))
                {
                    this.DeleteBackup(url);
                }
            }
        }

        private void DeleteBackup(string url)
        {
            var response = this.SendDeleteRequest(url);
            if (response.IsError)
            {
                if (response.Error.IsIgnorable)
                {
                    this.WriteWarning(response.Error.Message);
                }
                else
                {
                    this.WriteError(response.Error);
                }
            }
        }
    }
}
