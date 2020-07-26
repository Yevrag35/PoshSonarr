using MG.Sonarr.Functionality.Strings;
using MG.Sonarr.Results;
using System;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "IndexerOptions", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(IndexerOptions))]
    public class GetIndexerOptions : BaseSonarrCmdlet
    {
        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            IndexerOptions options = base.SendSonarrGet<IndexerOptions>(ApiEndpoints.IndexerOptions);
            base.SendToPipeline(options);
        }

        #endregion
    }
}