using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Commands;
using MG.Sonarr.Next.Models.Renames;
using MG.Sonarr.Next.Services.Jobs;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Commands
{
    [Cmdlet(VerbsLifecycle.Invoke, "SonarrRename", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
        DefaultParameterSetName = BY_EXPLICIT)]
    [Alias("Rename-SonarrEpisodeFile")]
    [MetadataCanPipe(Tag = Meta.CALENDAR)]
    [MetadataCanPipe(Tag = Meta.EPISODE)]
    [MetadataCanPipe(Tag = Meta.EPISODE_FILE)]
    [MetadataCanPipe(Tag = Meta.RENAMABLE)]
    public sealed class InvokeSonarrRenameCmdlet : SonarrCmdletBase
    {
        const string BY_EXPLICIT = "ByExplicit";
        const string BY_PIPELINE = "ByPipelineInput";
        List<IRenameFilePipeable> _renameables = null!;

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = BY_PIPELINE)]
        [ValidateNotNull]
        public IRenameFilePipeable[] InputObject { get; set; } = Array.Empty<IRenameFilePipeable>();

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = BY_EXPLICIT)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int SeriesId { get; set; }

        [Parameter(Mandatory = true, Position = 1, ParameterSetName = BY_EXPLICIT)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] EpisodeFileId { get; set; } = Array.Empty<int>();

        protected override void Process(IServiceProvider provider)
        {
            if (BY_PIPELINE == this.ParameterSetName)
            {
                _renameables ??= new(this.InputObject.Length);
                _renameables.AddRange(this.InputObject);
            }
        }

        protected override void End(IServiceProvider provider)
        {
            var client = provider.GetRequiredService<ICommandTracker>();

            if (!_renameables.IsNullOrEmpty())
            {
                this.SendRenamesFromPipeline(client);
            }
            else
            {
                this.SendRenameFromExplicit(client);
            }
        }

        private void SendRenameFromExplicit(ICommandTracker client)
        {
            PostRename rename = new()
            {
                FileIds = this.EpisodeFileId,
                SeriesId = this.SeriesId,
            };

            var response = client.SendRename(rename);
            _ = this.TryWriteObject(in response);
        }
        private void SendRenamesFromPipeline(ICommandTracker client)
        {
            IEnumerable<PostRename> renames = PostRename.FromRenameObjects(_renameables);

            foreach (PostRename rename in renames)
            {
                if (this.ShouldProcess(rename.ToString(), "Rename Episode Files"))
                {
                    var response = client.SendRename(rename);
                    _ = this.TryWriteObject(in response);
                }
            }
        }
    }
}
