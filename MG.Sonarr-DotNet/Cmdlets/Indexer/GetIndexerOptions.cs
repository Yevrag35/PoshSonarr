using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
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
            IndexerOptions options = base.SendSonarrGet<IndexerOptions>(ApiEndpoint.IndexerOptions);
            base.SendToPipeline(options);
        }

        #endregion
    }
}