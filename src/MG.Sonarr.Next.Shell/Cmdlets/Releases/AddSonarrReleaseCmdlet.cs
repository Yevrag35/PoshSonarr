using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Releases;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Releases
{
    [Cmdlet(VerbsCommon.Add, "SonarrRelease", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    [MetadataCanPipe(Tag = Meta.RELEASE)]
    public sealed class AddSonarrReleaseCmdlet : SonarrApiCmdletBase
    {
        MetadataTag Tag { get; set; } = null!;

        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        public ReleaseObject[] InputObject { get; set; } = Array.Empty<ReleaseObject>();

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            this.Tag = provider.GetRequiredService<IMetadataResolver>()[Meta.RELEASE];
        }
        protected override void Process(IServiceProvider provider)
        {
            foreach (ReleaseObject release in this.InputObject)
            {
                var post = ToPostRelease(release);

                if (this.ShouldProcess($"{post.Guid} from Indexer {post.IndexerId}", "Download Release"))
                {
                    this.SendRelease(post);
                }
            }
        }

        private void SendRelease(PostRelease post)
        {
            var response = this.SendPostRequest<PostRelease, PSObject>(this.Tag.UrlBase, post);

            if (response.IsT1)
            {
                this.WriteError(response.AsT1);
            }
            else
            {
                this.WriteObject(response.AsT0);
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
