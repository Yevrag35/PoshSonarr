using MG.Sonarr.Functionality.Strings;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Cmdlets
{
    public abstract class IndexerCmdlet : BaseSonarrCmdlet
    {
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            
        }

        protected List<Indexer> GetAllIndexers()
        {
            return base.SendSonarrListGet<Indexer>(ApiEndpoints.Indexer);
        }
        private Indexer GetIndexer(int id)
        {
            return base.SendSonarrGet<Indexer>(string.Format(ApiEndpoints.Indexer_ById, id));
        }
        protected IEnumerable<Indexer> GetIndexers(params int[] ids)
        {
            if (ids != null && ids.Length > 0)
            {
                foreach (int id in ids)
                {
                    yield return this.GetIndexer(id);
                }
            }
        }
        protected List<IndexerSchema> GetAllSchemas()
        {
            return base.SendSonarrListGet<IndexerSchema>(ApiEndpoints.IndexerSchema);
        }
    }
}
