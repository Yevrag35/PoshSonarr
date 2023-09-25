using MG.Sonarr.Next.Services;
using MG.Sonarr.Next.Services.Models.Tags;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Models.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

        protected override void Process()
        {
            SonarrTag tag = new() { Id = 0, Label = this.Label };

            string json = JsonSerializer.Serialize(tag, this.Options?.GetForDebugging());
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
