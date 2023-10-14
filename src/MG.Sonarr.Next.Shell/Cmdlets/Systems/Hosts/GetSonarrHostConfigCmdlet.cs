using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Hosts
{
    [Cmdlet(VerbsCommon.Get, "SonarrHostConfig")]
    [Alias("Get-SonarrHost")]
    public sealed class GetSonarrHostConfigCmdlet : SonarrApiCmdletBase
    {
        [Parameter]
        public SwitchParameter IncludeSensitiveInfo { get; set; }

        protected override void Process(IServiceProvider provider)
        {
            MetadataTag tag = provider.GetMetadataTag(Meta.HOST);
            this.WriteHostObject(tag, this.IncludeSensitiveInfo.ToBool());
        }

        private void WriteHostObject(MetadataTag tag, bool includeApiKey)
        {
            if (includeApiKey)
            {
                var response = this.GetHostResponse<HostObject>(tag);
                _ = this.TryWriteObject(in response);
            }
            else
            {
                var response = this.GetHostResponse<NoKeyHostObject>(tag);
                _ = this.TryWriteObject(in response);
            }
        }

        private SonarrResponse<THost> GetHostResponse<THost>(MetadataTag tag) where THost : HostObject
        {
            return this.SendGetRequest<THost>(tag.UrlBase);
        }
    }
}
