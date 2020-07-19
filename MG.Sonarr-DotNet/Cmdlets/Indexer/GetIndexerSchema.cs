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


        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (Context.IndexerSchemas == null)
            {
                List<IndexerSchema> schemas = base.SendSonarrListGet<IndexerSchema>(ApiEndpoint.IndexerSchema);
                Context.IndexerSchemas = IndexerSchemaCollection.FromSchemas(schemas);
            }
        }
        protected override void ProcessRecord()
        {
            base.SendToPipeline(Context.IndexerSchemas);
        }
    }
}
