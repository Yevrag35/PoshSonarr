using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Notification", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByName")]
    [OutputType(typeof(Notification))]
    [Alias("Get-Connection")]
    [CmdletBinding(PositionalBinding = false)]
    public class GetNotification : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/notification";
        private const string EP_WITH_ID = EP + "/{0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByName")]
        [SupportsWildcards]
        public string[] Name { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ById")]
        public int[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if ( ! base.HasParameterSpecified(this, x => x.Id) && this.TryGetAllNotifications(out List<Notification> allNotifs))
            {
                base.SendToPipeline(base.FilterByStringParameter(allNotifs, p => p.Name, this, cmd => cmd.Name));
            }
            else if (base.HasParameterSpecified(this, x => x.Id))
            {
                base.SendToPipeline(this.GetNotificationsById(this.Id));
            }
        }

        #endregion

        #region BACKEND METHODS
        private bool TryGetAllNotifications(out List<Notification> allNotifs)
        {
            allNotifs = base.SendSonarrListGet<Notification>(EP);
            return allNotifs != null && allNotifs.Count > 0;
        }
        private IEnumerable<Notification> GetNotificationsById(int[] ids)
        {
            foreach (int oneId in ids)
            {
                string endpoint = string.Format(EP_WITH_ID, oneId);
                Notification oneNotif = base.SendSonarrGet<Notification>(endpoint);
                if (oneNotif != null)
                    yield return oneNotif;
            }
        }

        #endregion
    }
}