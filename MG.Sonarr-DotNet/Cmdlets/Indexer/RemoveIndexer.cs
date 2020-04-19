using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Remove, "Indexer", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    public class RemoveIndexer : IndexerCmdlet
    {
        private bool _force;
        private HashSet<int> _toRemove;

        [Parameter(Mandatory = true, ParameterSetName = "ByInput", ValueFromPipeline = true)]
        public Indexer[] InputObject { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByExplicitId")]
        public int[] Id { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force
        {
            get => _force;
            set => _force = value;
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            
            if (this.ContainsParameter(x => x.Id))
            {
                IEnumerable<Indexer> chosen = base.GetIndexers(this.Id);
                _toRemove = new HashSet<int>(chosen.Select(x => x.Id));
            }
            else
            {
                _toRemove = new HashSet<int>();
            }
        }

        protected override void ProcessRecord()
        {
            if (this.ContainsParameter(x => x.InputObject))
                _toRemove.UnionWith(this.InputObject.Select(x => x.Id));
        }

        protected override void EndProcessing()
        {
            foreach (int id in _toRemove)
            {
                if (_force || base.FormatShouldProcess("Remove", "Indexer Id: {0}", id))
                {
                    base.SendSonarrDelete(string.Format(ApiEndpoint.Indexer_ById, id));
                }
            }
        }
    }
}
