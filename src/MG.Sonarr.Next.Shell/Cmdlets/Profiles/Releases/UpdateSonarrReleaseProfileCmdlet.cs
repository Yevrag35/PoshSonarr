using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Releases
{
    [Cmdlet(VerbsData.Update, "SonarrReleaseProfile", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [MetadataCanPipe(Tag = Meta.RELEASE_PROFILE)]
    public sealed class UpdateSonarrReleaseProfileCmdlet : SonarrMetadataCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [ValidateIds(ValidateRangeKind.Positive)]
        public ReleaseProfileObject[] InputObject { get; set; } = Array.Empty<ReleaseProfileObject>();

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.RELEASE_PROFILE];
        }

        protected override void Process(IServiceProvider provider)
        {
            foreach (ReleaseProfileObject profile in this.InputObject)
            {
                this.UpdateProfile(profile, this.Tag);
            }
        }

        private void UpdateProfile(ReleaseProfileObject profile, MetadataTag tag)
        {
            string url = tag.GetUrlForId(profile.Id);
            if (!this.ShouldProcess(url, "Update Release Profile"))
            {
                return;
            }

            var response = this.SendPutRequest(url, profile);
            _ = this.TryCommitFromResponse(profile, in response);
        }
    }
}

