using MG.Sonarr.Next.Services;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Models;
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

        protected override ErrorRecord? Process()
        {
            SonarrTag tag = new() { Id = 0, Label = this.Label };

            string json = JsonSerializer.Serialize(tag, this.Options?.GetForDebugging());
            if (this.ShouldProcess(json, "Creating Tag"))
            {
                var oneOf = this.SendPostRequest<SonarrTag, SonarrTag>(Constants.TAG, tag);
                return this.WriteSonarrResult(oneOf);
            }

            return null;
        }
    }
}
