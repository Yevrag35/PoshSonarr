using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.RemotePaths;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.RemotePath
{
    [Cmdlet(VerbsData.Update, "SonarrRemotePathMapping", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [Alias("Update-SonarrRemoteMapping", "Set-SonarrRemotePathMapping", "Set-SonarrRemoteMapping")]
    [MetadataCanPipe(Tag = Meta.REMOTE_PATH_MAPPING)]
    public sealed class UpdateSonarrRemotePathMappingCmdlet : SonarrMetadataCmdlet
    {
        RemotePathBody? _body;

        protected override bool CaptureDebugPreference => true;

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string HostName
        {
            get => _body?.Host ?? string.Empty;
            set => this.SetBodyParameter(value, (x, s) => x.Host = s);
        }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int Id
        {
            get => _body?.Id ?? 0;
            set => this.SetBodyParameter(value);
        }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string LocalPath
        {
            get => _body?.LocalPath ?? string.Empty;
            set => this.SetBodyParameter(value, (x, s) => x.LocalPath = s);
        }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string RemotePath
        {
            get => _body?.RemotePath ?? string.Empty;
            set => this.SetBodyParameter(value, (x, s) => x.RemotePath = s);
        }

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.REMOTE_PATH_MAPPING];
        }

        protected override void Process(IServiceProvider provider)
        {
            if (_body is null)
            {
                return;
            }

            this.SerializeIfDebug(_body);
            string url = this.Tag.GetUrlForId(this.Id);

            if (!this.ShouldProcess(url, "Updating Remote Path Mapping"))
            {
                return;
            }

            var response = this.SendPutRequest(url, _body);
            if (response.IsError)
            {
                this.WriteError(response.Error);
                return;
            }

            this.WriteVerbose($"Updated Remote Path Mapping -> {this.Id}");
        }

        private void SetBodyParameter(int id)
        {
            if (id <= 0)
            {
                return;
            }

            _body ??= new();
            _body.Id = id;
        }
        private void SetBodyParameter(string value, Action<RemotePathBody, string> action)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            _body ??= new();
            action(_body, value);
        }
    }
}

