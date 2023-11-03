using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Releases
{
    [Cmdlet(VerbsCommon.Remove, "SonarrReleaseProfile", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true,
        DefaultParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
    [MetadataCanPipe(Tag = Meta.RELEASE_PROFILE)]
    public sealed class RemoveSonarrReleaseProfile : SonarrApiCmdletBase
    {
        MetadataTag _tag = null!;

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int Id { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = PSConstants.PSET_PIPELINE, ValueFromPipeline = true)]
        [ValidateId(ValidateRangeKind.Positive)]
        public ReleaseProfileObject? InputObject
        {
            get => null;
            set => this.Id = value?.Id ?? -1;
        }

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _tag = provider.GetMetadataTag(Meta.RELEASE_PROFILE);
        }

        protected override void Process(IServiceProvider provider)
        {
            string url = _tag.GetUrlForId(this.Id);

            if (!this.Force
                &&
                !this.ShouldProcess(url, "Deleting Release Profile"))
            {
                return;
            }

            var response = this.SendDeleteRequest(url);
            if (response.IsError)
            {
                this.WriteError(response.Error);
                return;
            }

            this.WriteVerbose($"Deleted Release Profile -> {this.Id}");
        }
    }
}

