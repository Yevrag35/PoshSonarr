using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "IndexerSchema", ConfirmImpact = ConfirmImpact.None)]
    public class GetIndexerSchema : IndexerCmdlet
    {


        protected override void BeginProcessing() => base.BeginProcessing();
        protected override void ProcessRecord()
        {
            List<IndexerSchema> schemas = base.GetAllSchemas();
            base.SendToPipeline(schemas);
        }
    }
}
