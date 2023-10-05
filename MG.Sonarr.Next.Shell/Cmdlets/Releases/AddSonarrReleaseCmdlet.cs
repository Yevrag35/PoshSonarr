using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Releases;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Releases
{
    [Cmdlet(VerbsCommon.Add, "SonarrRelease", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    public sealed class AddSonarrReleaseCmdlet : SonarrApiCmdletBase
    {
        MetadataTag Tag { get; set; } = null!;

        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        public ReleaseObject[] InputObject { get; set; } = Array.Empty<ReleaseObject>();

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            this.Tag = provider.GetRequiredService<MetadataResolver>()[Meta.RELEASE];
        }
        protected override void Process(IServiceProvider provider)
        {
            foreach (ReleaseObject release in this.InputObject)
            {

                var response = this.SendPostRequest<PostRelease, PSObject>(this.Tag.UrlBase, ToPostRelease(release));

                if (response.IsT1)
                {
                    this.WriteError(response.AsT1);
                }
                else
                {
                    this.WriteObject(response.AsT0);
                }
            }
        }

        private static PostRelease ToPostRelease(ReleaseObject release)
        {
            return new()
            {
                Guid = release.ReleaseUrl,
                IndexerId = release.IndexerId,
            };
        }
    }
}
