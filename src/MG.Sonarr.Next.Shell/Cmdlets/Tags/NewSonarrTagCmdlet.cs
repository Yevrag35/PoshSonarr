using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Models.Tags;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags
{
    [Cmdlet(VerbsCommon.New, "SonarrTag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    public sealed class NewSonarrTagCmdlet : SonarrApiCmdletBase
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        [Alias("Name")]
        public string Label { get; set; } = null!;

        protected override void Process(IServiceProvider provider)
        {
            SonarrTag tag = new() { Id = 0, Label = this.Label };

            string json = JsonSerializer.Serialize(
                value: tag,
                options: provider.GetService<ISonarrJsonOptions>()?.ForDebugging);

            if (this.ShouldProcess(json, "Creating Tag"))
            {
                var oneOf = this.SendPostRequest<SonarrTag, TagObject>(Constants.TAG, tag);
                if (oneOf.TryPickT0(out TagObject? to, out var error))
                {
                    this.WriteObject(to);
                }
                else
                {
                    this.WriteError(error);
                }
            }
        }
    }
}
