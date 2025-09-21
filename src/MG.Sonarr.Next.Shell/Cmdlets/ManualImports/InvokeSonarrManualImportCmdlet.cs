using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.ManualImports;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Exceptions;

namespace MG.Sonarr.Next.Shell.Cmdlets.ManualImports
{
    [Cmdlet(VerbsLifecycle.Invoke, "SonarrManualImport", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [MetadataCanPipe(Tag = Meta.MANUAL_IMPORT)]
    public class InvokeSonarrManualImportCmdlet : SonarrMetadataCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true), AllowEmptyCollection]
        public ManualImportObject[] InputObject { get; set; } = [];

        [Parameter]
        public ManualImportMode ImportMode { get; set; } = ManualImportMode.Copy;

        private MetadataList<ManualImportObject> _list = null!;
        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.COMMAND];
        }

        [SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in nameof()")]
        protected override void Process(IServiceProvider provider)
        {
            if (this.InputObject.Length == 0)
                return;

            foreach (ManualImportObject obj in this.InputObject)
            {
                if (!obj.IsReadyToPost())
                {
                    this.WriteError(new ErrorRecord(new SonarrParameterException(nameof(InputObject), ParameterErrorType.Malformed, "The import is not ready to post as it's missing properties."), 
                        "InvokeSonarrManualImport:ImportNotReady", ErrorCategory.InvalidArgument, obj));

                    continue;
                }

                if (this.ShouldProcess(this.Tag.UrlBase, $"Manual Import to Series '{obj.Series?.Title}' as episode number {obj.Episodes?.FirstOrDefault()?.AbsoluteEpisodeNumber}"))
                {
                    _list ??= new(1);
                    _list.Add(obj);
                }
            }
        }

        protected override void End(IServiceProvider provider)
        {
            if (_list is null or { Count: 0 })
                return;

            var body = new
            {
                Files = _list.ToManualImportFiles(),
                ImportMode = GetImportModeString(this.ImportMode),
                Name = "ManualImport",
            };

            this.SerializeIfDebug(body, includeType: false);

            SonarrClientResult response = this.SendPostRequest(this.Tag.UrlBase, body);
            if (response.IsError)
            {
                this.WriteConditionalError(response.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _list?.Clear();
            _list = null!;
            base.Dispose(disposing);
        }

        private static string GetImportModeString(ManualImportMode mode)
        {
            return mode switch
            {
                ManualImportMode.Copy => "copy",
                ManualImportMode.Move => "move",
                _ => string.Empty,
            };
        }
    }

    public enum ManualImportMode
    {
        Copy,
        Move,
    }
}