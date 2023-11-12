using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Delay
{
    [Cmdlet(VerbsData.Update, "SonarrDelayProfile", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [MetadataCanPipe(Tag = Meta.DELAY_PROFILE)]
    public sealed class UpdateSonarrDelayProfileCmdlet : SonarrMetadataCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public DelayProfileObject[] InputObject { get; set; } = Array.Empty<DelayProfileObject>();

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.DELAY_PROFILE];
        }

        protected override void Process(IServiceProvider provider)
        {
            foreach (DelayProfileObject profile in this.InputObject)
            {
                this.UpdateProfile(profile, this.Tag);
            }
        }

        private void UpdateProfile(DelayProfileObject profile, MetadataTag tag)
        {
            string url = tag.GetUrlForId(profile.Id);
            if (!this.ShouldProcess(url, "Update Delay Profile"))
            {
                return;
            }

            var response = this.SendPutRequest(url, profile);
            _ = this.TryCommitFromResponse(profile, in response);
        }
    }
}

