using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Episodes;
using System.Management.Automation.Language;

namespace MG.Sonarr.Next.Shell.Cmdlets.Episodes
{
    [Cmdlet(VerbsData.Update, "SonarrEpisode", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    public sealed class UpdateSonarrEpisodeCmdlet : SonarrApiCmdletBase
    {
        [Parameter(Mandatory = true, ParameterSetName = "ByPipelineInput")]
        public EpisodeObject[] InputObject { get; set; } = Array.Empty<EpisodeObject>();

        protected override void Process(IServiceProvider provider)
        {
            this.InputObject ??= Array.Empty<EpisodeObject>();

            foreach (var ep in this.InputObject)
            {
                if (this.IsValid(ep))
                {
                    this.SendUpdate(ep);
                }
            }
        }

        private bool IsValid([NotNullWhen(true)] IHasId? value)
        {
            if (value is null || value.Id <= 0)
            {
                this.WriteWarning("An episode with an invalid ID was passed. It will be ignored.");
                return false;
            }

            return true;
        }
        private void SendUpdate(EpisodeObject episode)
        {
            string url = episode.MetadataTag.GetUrlForId(episode.Id);
            if (this.ShouldProcess(url, "Update Episode"))
            {
                var response = this.SendPutRequest(url, episode);
                if (response.IsError)
                {

                }
            }
        }
    }
}
