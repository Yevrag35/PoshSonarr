using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.RootFolders
{
    [Cmdlet(VerbsCommon.Get, "SonarrRootFolder")]
    public sealed class GetSonarrRootFolderCmdlet : SonarrApiCmdletBase
    {
        MetadataTag Tag { get; }

        public GetSonarrRootFolderCmdlet()
            : base()
        {
            this.Tag = this.Services.GetRequiredService<MetadataResolver>()[Meta.ROOT_FOLDER];
        }

        [Parameter(Mandatory = false, Position = 0)]
        public int[] Id { get; set; } = Array.Empty<int>();

        protected override void Process()
        {
            IEnumerable<PSObject> rootFolders = this.SendRequests(this.Id);
            this.WriteCollection(this.Tag, rootFolders);
        }

        private IEnumerable<PSObject> SendRequests(int[] ids)
        {
            if (ids.Length <= 0)
            {
                var allResponse = this.SendGetRequest<List<PSObject>>(this.Tag.UrlBase);
                if (allResponse.IsError)
                {
                    this.StopCmdlet(allResponse.Error);
                    yield break;
                }

                foreach (var pso in allResponse.Data)
                {
                    yield return pso;
                }

                yield break;
            }

            foreach (int id in ids)
            {
                string url = this.Tag.GetUrlForId(id);
                var response = this.SendGetRequest<PSObject>(url);
                if (response.IsError)
                {
                    this.WriteConditionalError(response.Error);
                    continue;
                }

                yield return response.Data;
            }
        }
    }
}
