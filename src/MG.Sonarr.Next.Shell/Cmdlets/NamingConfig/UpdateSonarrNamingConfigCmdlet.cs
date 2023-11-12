using MG.Sonarr.Next.Models.Config;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.NamingConfig
{
    [Cmdlet(VerbsData.Update, "SonarrNamingConfig", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    public sealed class UpdateSonarrNamingConfigCmdlet : SonarrApiCmdletBase
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [ValidateNotNull]
        [ValidateId(ValidateRangeKind.Positive)]
        public NamingConfigObject InputObject { get; set; } = null!;

        protected override void Process(IServiceProvider provider)
        {
            var tag = provider.GetMetadataTag(Meta.NAMING_CONFIG);
            string url = tag.GetUrlForId(this.InputObject.Id);

            if (!this.ShouldProcess(url, "Update Naming Config"))
            {
                return;
            }

            var response = this.SendPutRequest(url, this.InputObject);
            bool committed = this.TryCommitFromResponse(this.InputObject, in response);
            Debug.Assert(committed);
        }
    }
}

