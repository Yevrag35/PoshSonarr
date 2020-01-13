using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Remove, "QueueItem", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    [CmdletBinding(PositionalBinding = false, DefaultParameterSetName = "ViaPipeline")]
    public class RemoveQueueItem : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP_FORMAT = "/queue/{0}";
        private List<long> _ids;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ParameterSetName = "ViaPipeline", ValueFromPipelineByPropertyName = true, DontShow = true)]
        public long QueueItemId { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByQueueItemIds")]
        public long[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (base.HasParameterSpecified(this, x => x.Id))
                _ids = new List<long>(this.Id);

            else
                _ids = new List<long>();
        }

        protected override void ProcessRecord()
        {
            if (base.HasParameterSpecified(this, x => x.QueueItemId))
            {
                _ids.Add(this.QueueItemId);
            }
        }

        protected override void EndProcessing()
        {
            for (int i = 0; i < _ids.Count; i++)
            {
                long oneId = _ids[i];
                if (base.ShouldProcess(string.Format("QueueItemId: {0}", oneId), "Remove"))
                {
                    base.SendSonarrDelete(string.Format(EP_FORMAT, oneId));
                }
            }
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}