using MG.Posh.Extensions.Bound;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Remove, "QueueItem", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    [CmdletBinding(PositionalBinding = false, DefaultParameterSetName = "ByQueueItemIds")]
    public class RemoveQueueItem : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP_FORMAT = "/queue/{0}";
        private bool _force;
        private HashSet<long> _ids;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ParameterSetName = "ViaPipeline", ValueFromPipeline = true)]
        public QueueItem InputObject { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByQueueItemIds")]
        public long[] Id { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force
        {
            get => _force;
            set => _force = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (this.ContainsParameter(x => x.Id))
            {
                _ids = new HashSet<long>(this.Id);
            }
            else
            {
                _ids = new HashSet<long>();
            }
        }

        protected override void ProcessRecord()
        {
            if (this.ContainsParameter(x => x.InputObject))
            {
                _ids.Add(this.InputObject.Id);
            }
        }

        protected override void EndProcessing()
        {
            foreach (long id in _ids)
            {
                if (_force || base.FormatShouldProcess("Remove", "Queue Item Id: {0}", id))
                {
                    base.SendSonarrDelete(string.Format(EP_FORMAT, id));
                }
            }
            //for (int i = 0; i < _ids.Count; i++)
            //{
            //    long oneId = _ids[i];
            //    if (base.ShouldProcess(string.Format("QueueItemId: {0}", oneId), "Remove"))
            //    {
            //        base.SendSonarrDelete(string.Format(EP_FORMAT, oneId));
            //    }
            //}
        }

        #endregion
    }
}