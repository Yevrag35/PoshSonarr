using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.DownloadClients;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Output;

namespace MG.Sonarr.Next.Shell.Cmdlets.DownloadClients
{
    [Cmdlet(VerbsCommon.New, "SonarrDownloadClient", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [MetadataCanPipe(Tag = Meta.DOWNLOAD_CLIENT_SCHEMA), OutputType(typeof(IDownloadClientOutput))]
    public sealed class NewSonarrDownloadClientCmdlet : SonarrMetadataCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public DownloadClientSchemaObject Schema { get; set; } = null!;

        [Parameter]
        public SwitchParameter Enabled { get; set; }

        [Parameter]
        public SwitchParameter ForceSave { get; set; }

        [Parameter]
        public string? Name { get; set; }

        [Parameter]
        public int? Priority { get; set; }

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.DOWNLOAD_CLIENT];
        }

        [SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in nameof()")]
        protected override void Process(IServiceProvider provider)
        {
            DownloadClientSchemaObject schema = (DownloadClientSchemaObject)this.Schema.Copy();
            if (this.HasNotNullParameter(Name))
            {
                schema.Name = this.Name;
            }

            if (this.HasParameter(Priority) && this.Priority.HasValue)
            {
                schema.Priority = this.Priority.Value;
            }

            if (this.Enabled.IsPresent && this.Enabled != schema.Enable)
            {
                schema.Enable = this.Enabled;
            }

            if (string.IsNullOrWhiteSpace(schema.Name))
            {
                var ex = new MalformedSonarrObjectException("The download client schema must have a valid Name.", schema);
                this.WriteError(new ErrorRecord(ex, "New-SonarrDownloadClient.InvalidDownloadClientName", ErrorCategory.InvalidData, schema));
                return;
            }

            this.SerializeIfDebug(schema, includeType: true);

            if (this.ShouldProcess(this.Tag.UrlBase, $"Create new download client of type '{schema.ImplementationName}' -> '{schema.Name}'"))
            {
                string url = this.ForceSave.ToBool()
                    ? this.Tag.GetUrl(BooleanQueryField.CreateTrue("forceSave"))
                    : this.Tag.UrlBase;

                var response = this.SendPostRequest<DownloadClientSchemaObject, DownloadClientObject>(url, schema);

                this.WriteOutcome(response);
            }
        }
    }
}
