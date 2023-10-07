using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Episodes
{
    [Cmdlet(VerbsCommon.Remove, "SonarrEpisodeFile", SupportsShouldProcess = true)]
    public sealed class RemoveSonarrEpisodeFileCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByEpisodeId")]
        public int[] Id { get; set; } = Array.Empty<int>();

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByPipelineInput")]
        public IEpisodeFilePipeable[] InputObject { get; set; } = Array.Empty<IEpisodeFilePipeable>();

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override int Capacity => 1;
        protected override MetadataTag GetMetadataTag(MetadataResolver resolver)
        {
            return resolver[Meta.EPISODE_FILE];
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
            if (this.HasParameter(x => x.InputObject))
            {
                _ids.UnionWith(
                    this.InputObject.Select(x => x.EpisodeFileId)
                                    .Where(x => x > 0));
            }
        }
        protected override void End(IServiceProvider provider)
        {
            bool yesToAll = false;
            bool noToAll = false;

            bool force = this.Force.ToBool();

            foreach (int id in _ids)
            {
                string url = this.Tag.GetUrlForId(id);
                if (this.ShouldProcess(url, "Deleting Episode File")
                    &&
                    (force
                    ||
                    this.ShouldContinue(query: "Are you sure you want to delete the episode file?", caption: $"Delete Episode file: {id}", yesToAll: ref yesToAll, noToAll: ref noToAll)))
                {
                    this.DeleteEpisodeFile(url);
                }
            }
        }

        private void DeleteEpisodeFile(string url)
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
