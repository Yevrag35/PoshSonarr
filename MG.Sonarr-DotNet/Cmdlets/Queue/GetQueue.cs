using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Queue", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(QueueItem))]
    public class GetQueue : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/queue";
        private const string EP_ID = EP + "/{0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0)]
        public long[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (base.HasParameterSpecified(this, x => x.Id))
                base.WriteObject(this.GetQueueItemsById(this.Id), true);

            else if (this.TryGetAllQueueItems(out List<QueueItem> allItems))
                base.WriteObject(allItems, true);
        }

        #endregion

        #region METHODS
        private bool TryGetAllQueueItems(out List<QueueItem> allItems)
        {
            allItems = base.SendSonarrListGet<QueueItem>(EP);
            return allItems != null && allItems.Count > 0;
        }
        private IEnumerable<QueueItem> GetQueueItemsById(long[] ids)
        {
            foreach (int id in ids)
            {
                string ep = string.Format(EP_ID, id);
                QueueItem qi = base.SendSonarrGet<QueueItem>(ep);
                if (qi != null)
                {
                    yield return qi;
                }
            }
        }

        #endregion
    }
}