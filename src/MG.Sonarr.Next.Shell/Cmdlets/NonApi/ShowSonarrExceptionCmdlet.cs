using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Services.Collections;
using MG.Sonarr.Next.Services.Exceptions;
using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Models;
using MG.Sonarr.Next.Shell.Extensions;
using System.Collections;

namespace MG.Sonarr.Next.Shell.Cmdlets.NonApi
{
    [Cmdlet(VerbsCommon.Show, "SonarrException")]
    [Alias("Show-SonarrError")]
    public sealed class ShowSonarrExceptionCmdlet : SonarrCmdletBase
    {
        const string ERROR = "error";
        bool ProcessedPipeline { get; set; }

        [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Alias("Exception")]
        public SonarrHttpException InputObject { get; set; } = null!;

        protected override void Process(IServiceProvider provider)
        {
            if (this.InputObject is not null)
            {
                this.ProcessedPipeline = true;
                this.WriteDetails(this.InputObject);
            }
        }
        protected override void End(IServiceProvider provider)
        {
            if (!this.ProcessedPipeline)
            {
                if (TryGetLastError(this.SessionState.PSVariable, out SonarrHttpException? exception))
                {
                    this.WriteDetails(exception);
                }
                else
                {
                    this.WriteVerbose("No readable exceptions have been recorded.");
                }
            }
        }

        private void WriteDetails(SonarrHttpException exception)
        {
            IErrorCollection errors = exception.ExtendedInfo;
            if (errors.Count <= 0)
            {
                return;
            }

            foreach (SonarrObject pso in errors)
            {
                PSObject copy = pso.Copy();
                copy.AddProperty(nameof(HttpRequestMessage.RequestUri), exception.RequestUri);

                this.WriteObject(copy);
            }
        }

        private static bool TryGetLastError(PSVariableIntrinsics intrinsics, [NotNullWhen(true)] out SonarrHttpException? result)
        {
            result = null;
            if (!intrinsics.TryGetVariableValue(ERROR, out ICollection? collection) || collection.Count <= 0)
            {
                return false;
            }

            IEnumerable<SonarrErrorRecord> records = collection.OfType<SonarrErrorRecord>();
            SonarrErrorRecord? record = records.FirstOrDefault();

            if (record is not null && record.Exception is SonarrHttpException httpEx)
            {
                result = httpEx;
                return true;
            }

            return false;
        }
    }
}
