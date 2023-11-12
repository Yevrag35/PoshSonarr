using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Hosts
{
    [Cmdlet(VerbsData.Update, "SonarrHostConfig", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [Alias("Set-SonarrHostConfig", "Update-SonarrHost", "Set-SonarrHost")]
    [MetadataCanPipe(Tag = Meta.HOST)]
    public sealed class UpdateSonarrHostConfigCmdlet : SonarrApiCmdletBase
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [ValidateNotNull]
        [ValidateId(ValidateRangeKind.Positive)]
        public HostObject InputObject { get; set; } = null!;

        protected override void Process(IServiceProvider provider)
        {
            MetadataTag tag = provider.GetMetadataTag(Meta.HOST);

            if (this.ShouldProcess(tag.UrlBase, "Update Host Config"))
            {
                var response = this.SendPutRequest(tag.UrlBase, this.InputObject);
                if (response.IsError)
                {
                    this.InputObject.Reset();
                    this.WriteError(response.Error);
                }
                else
                {
                    this.InputObject.Commit();
                }
            }
        }
    }
}
