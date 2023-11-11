using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.RemotePaths;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using System.CodeDom;

namespace MG.Sonarr.Next.Shell.Cmdlets.RemotePath
{
    [Cmdlet(VerbsCommon.New, "SonarrRemotePathMapping", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [Alias("New-SonarrRemotePath", "New-SonarrRemoteMapping")]
    public sealed class NewSonarrRemotePathMappingCmdlet : SonarrMetadataCmdlet
    {
        const string USERDNSDOMAIN = "USERDNSDOMAIN";
        const string LOCALHOST = "localhost";
        const string DOT_HOST = ".";

        protected override bool CaptureDebugPreference => true;

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string HostName { get; set; } = string.Empty;

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string RemotePath { get; set; } = string.Empty;

        [Parameter(Mandatory = true)]
        [ValidateIsLocalPath(AllowRelative = true, MustExist = false)]
        public string LocalPath { get; set; } = string.Empty;

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.REMOTE_PATH_MAPPING];
        }

        protected override void Begin(IServiceProvider provider)
        {
            if (LOCALHOST.Equals(this.HostName, StringComparison.InvariantCultureIgnoreCase) || DOT_HOST.Equals(this.HostName))
            {
                this.LocalPath = this.GetResolvedPath(this.LocalPath);
                this.HostName = GetLocalHostName(Environment.MachineName);
            }
        }
        protected override void Process(IServiceProvider provider)
        {
            RemotePathBody newMap = new()
            {
                Host = this.HostName,
                RemotePath = this.RemotePath,
                LocalPath = this.LocalPath,
            };

            this.SerializeIfDebug(newMap, includeType: false);
            if (!this.ShouldProcess(this.Tag.UrlBase, "Creating Remote Path Mapping"))
            {
                return;
            }

            this.SendNewRequest(newMap, this.Tag);
        }

        private static string GetLocalHostName(string localHostName)
        {
            string? domain = Environment.GetEnvironmentVariable(USERDNSDOMAIN);
            if (string.IsNullOrWhiteSpace(domain))
            {
                return localHostName;
            }

            int length = localHostName.Length + domain.Length + 1;
            return string.Create(length, (localHostName, domain), (chars, state) =>
            {
                state.localHostName.CopyTo(chars);
                int position = state.localHostName.Length;

                chars[position++] = '.';

                scoped Span<char> domain = chars.Slice(position);
                state.domain.AsSpan().ToLower(domain, Statics.DefaultCulture);
            });
        }
        private void SendNewRequest<T>(T body, MetadataTag tag) where T : notnull
        {
            var oneOf = this.SendPostRequest<T, RemotePathObject>(tag.UrlBase, body);
            if (oneOf.TryPickT1(out var error, out var remotePath))
            {
                this.WriteError(error);
            }
            else
            {
                this.WriteObject(remotePath);
            }
        }
    }
}
