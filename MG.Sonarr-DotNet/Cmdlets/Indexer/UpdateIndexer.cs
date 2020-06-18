using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsData.Update, "Indexer", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [OutputType(typeof(Indexer))]
    public class UpdateIndexer : IndexerCmdlet
    {
        private bool _force;
        private bool _passThru;

        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        public Indexer[] InputObject { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force
        {
            get => _force;
            set => _force = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru
        {
            get => _passThru;
            set => _passThru = value;
        }

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            IEnumerable<Indexer> updated = this.PutAndYield(this.InputObject);
            if (_passThru)
                base.SendToPipeline(updated);
        }

        private IEnumerable<Indexer> PutAndYield(IEnumerable<Indexer> update)
        {
            foreach (Indexer indexer in this.InputObject)
            {
                if (_force || base.FormatShouldProcess("Update", "Indexer: {0}", indexer.Name))
                {
                    yield return base.SendSonarrPut<Indexer>(string.Format(ApiEndpoint.Indexer_ById, indexer.Id), indexer);
                }
            }
        }
    }
}
