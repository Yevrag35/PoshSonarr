using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Indexer", ConfirmImpact = ConfirmImpact.None,
        DefaultParameterSetName = "None")]
    public class GetIndexer : IndexerCmdlet
    {
        [Parameter(Mandatory = true, ParameterSetName = "ByIndexerId")]
        public int[] Id { get; set; }

        [Parameter(Mandatory = false, Position = 0)]
        [SupportsWildcards]
        public string[] Name { get; set; }

        [Parameter(Mandatory = false)]
        public DownloadProtocol Protocol { get; set; }

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.ContainsParameter(x => x.Id))
            {
                base.SendToPipeline(base.GetIndexers(this.Id));
            }
            else
            {
                List<Indexer> indexers = base.GetAllIndexers();
                base.SendToPipeline(indexers
                    .ThenFilterByStrings(this,
                        x => x.Name,
                        m => m.Name,
                        this.Name)
                    .ThenFilterBy(this,
                        p => p.Protocol,
                        null,
                        w => this.Protocol.Equals(w.Protocol)));
            }
        }
    }
}
