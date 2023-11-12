using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags
{
    [Cmdlet(VerbsCommon.Remove, "SonarrTag", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High,
        DefaultParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
    [Alias("Delete-SonarrTag")]
    [MetadataCanPipe(Tag = Meta.TAG)]
    public sealed class RemoveSonarrTagCmdlet : SonarrApiCmdletBase
    {
        MetadataTag _tag = null!;

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int Id { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSET_PIPELINE)]
        [ValidateId(ValidateRangeKind.Positive)]
        public TagObject? InputObject
        {
            get => null;
            set => this.Id = value?.Id ?? -1;
        }

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _tag = provider.GetRequiredService<IMetadataResolver>()[Meta.TAG];
        }
        protected override void Process(IServiceProvider provider)
        {
            if (this.InvokeCommand.HasErrors || this.Id <= 0)
            {
                return;
            }

            string url = _tag.GetUrlForId(this.Id);
            if (!this.Force
                &&
                !this.ShouldProcess(url, "Delete Tag"))
            {
                return;
            }

            var response = this.SendDeleteRequest(url);
            if (response.IsError)
            {
                this.WriteError(response.Error);
                return;
            }

            this.WriteVerbose($"Deleted Tag -> {this.Id}");
        }
    }
}
